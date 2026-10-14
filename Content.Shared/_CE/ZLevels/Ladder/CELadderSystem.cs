using System.Numerics;
using Content.Shared._CE.ZLevels.Core.Components;
using Content.Shared._CE.ZLevels.Core.EntitySystems;
using Content.Shared._CE.ZLevels.Flight.Components;
using Content.Shared.ActionBlocker;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Systems;
using Content.Shared.Standing;
using Robust.Shared.Analyzers;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Physics.Events;
using Robust.Shared.Player;

namespace Content.Shared._CE.ZLevels.Ladder;

/// <summary>
/// Vertical movement on wall ladders. Grip is tracked through the ladder's sensor fixture and carried
/// over to the level above while climbing out. Velocity is set through <see cref="CEGetZVelocityEvent"/>,
/// so everything runs inside the predicted z-physics step.
/// </summary>
public sealed partial class CELadderSystem : EntitySystem
{
    [Dependency] private INetManager _net = default!;
    [Dependency] private ISharedPlayerManager _player = default!;
    [Dependency] private ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private CESharedZLevelsSystem _zLevel = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedMoverController _mover = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private StandingStateSystem _standing = default!;


    [Dependency] private EntityQuery<CELadderComponent> _ladderQuery = default!;
    [Dependency] private EntityQuery<CEZFlyerComponent> _flyerQuery = default!;
    [Dependency] private EntityQuery<CEZPhysicsComponent> _zPhysicsQuery = default!;
    [Dependency] private EntityQuery<InputMoverComponent> _moverQuery = default!;
    [Dependency] private EntityQuery<MovementSpeedModifierComponent> _speedQuery = default!;

    /// <summary>
    /// Minimal input component towards the wall that counts as "climbing".
    /// </summary>
    private const float ClimbInputThreshold = 0.1f;

    /// <summary>
    /// Below this distance to the ground the climber is considered standing on the floor.
    /// </summary>
    private const float GroundTolerance = 0.05f;

    [SubscribeLocalEvent]
    private void OnStartCollide(Entity<CELadderComponent> ent, ref StartCollideEvent args)
    {
        // Only the mob's main hard fixture counts, so extra sensor fixtures can't hold on by themselves.
        if (!args.OtherFixture.Hard
            || !_moverQuery.HasComp(args.OtherEntity)
            || !_zPhysicsQuery.HasComp(args.OtherEntity))
            return;

        var climber = EnsureComp<CELadderClimberComponent>(args.OtherEntity);
        if (!climber.Ladders.Add(ent))
            return;

        Dirty(args.OtherEntity, climber);
        _zLevel.RefreshVelocityRaiseEvent(args.OtherEntity);
        _zLevel.WakeBody(args.OtherEntity);
    }

    [SubscribeLocalEvent]
    private void OnEndCollide(Entity<CELadderComponent> ent, ref EndCollideEvent args)
    {
        if (!args.OtherFixture.Hard
            || !TryComp<CELadderClimberComponent>(args.OtherEntity, out var climber)
            || !climber.Ladders.Remove(ent))
            return;

        Dirty(args.OtherEntity, climber);
        TryLetGo((args.OtherEntity, climber));
    }

    [SubscribeLocalEvent]
    private void OnBeforeMapMove(Entity<CELadderClimberComponent> ent, ref CEZLevelBeforeMapMoveEvent args)
    {
        // Climbing out keeps holding the ladder we came up on; going down drops it and the sensors below take over.
        EntityUid? below = null;
        if (args.Offset > 0 && TryGetLadderOnOwnLevel(ent, out var ladder))
            below = ladder;

        ent.Comp.BelowLadder = below;
        Dirty(ent);
    }

    [SubscribeLocalEvent]
    private void OnClimberShutdown(Entity<CELadderClimberComponent> ent, ref ComponentShutdown args)
    {
        _zLevel.RefreshVelocityRaiseEvent(ent.Owner);
    }

    [SubscribeLocalEvent]
    private void OnRefreshVelocityRaise(Entity<CELadderClimberComponent> ent, ref CERefreshVelocityRaiseEvent args)
    {
        if (ent.Comp.LifeStage <= ComponentLifeStage.Running)
            args.Raise = true;
    }

    [SubscribeLocalEvent]
    private void OnMoveInput(Entity<CELadderClimberComponent> ent, ref MoveInputEvent args)
    {
        // Pushing into the wall doesn't move the body, so nothing else would wake it up to start climbing.
        if (args.HasDirectionalMovement)
            _zLevel.WakeBody(ent.Owner);
    }

    [SubscribeLocalEvent]
    private void OnGetZVelocity(Entity<CELadderClimberComponent> ent, ref CEGetZVelocityEvent args)
    {
        if (!TryGetGrip(ent, out var ladder, out var fromBelow))
            return;

        if (_flyerQuery.TryComp(ent, out var flyer) && flyer.Active)
            return;

        if (!_moverQuery.TryComp(ent, out var mover)
            || !_actionBlocker.CanMove(ent, mover)
            || _standing.IsDown(ent.Owner))
            return;

        var zPhys = args.Target.Comp;
        var outward = _transform.GetWorldRotation(ladder).ToWorldVec();

        float target;
        if (Vector2.Dot(GetInputDirection(mover), -outward) > ClimbInputThreshold)
        {
            target = GetMoveSpeed(ent, mover) * ladder.Comp.ClimbSpeedModifier;

            if (fromBelow && zPhys.LocalPosition >= ladder.Comp.ExitHeight)
                target = 0f;
        }
        else
        {
            target = mover.Sprinting ? -ladder.Comp.DescendSpeed : 0f;
        }

        if (target <= 0f && zPhys.LocalPosition - zPhys.CachedGroundHeight <= GroundTolerance)
        {
            // Standing on the floor: leave it to the regular ground handling so the body can fall asleep.
            if (fromBelow)
                ClearBelowLadder(ent);

            return;
        }

        args.VelocityDelta = (target - zPhys.Velocity) / args.FrameTime;
        TryPlayStepSound(ent, ladder, zPhys, target * args.FrameTime);
    }

    private bool TryGetGrip(Entity<CELadderClimberComponent> ent, out Entity<CELadderComponent> ladder, out bool fromBelow)
    {
        fromBelow = false;

        if (TryGetLadderOnOwnLevel(ent, out ladder))
            return true;

        if (ent.Comp.BelowLadder is not { } below)
            return false;

        if (_ladderQuery.TryComp(below, out var belowComp) && IsDirectlyBelow(ent, below))
        {
            ladder = (below, belowComp);
            fromBelow = true;
            return true;
        }

        ClearBelowLadder(ent);
        return false;
    }

    /// <summary>
    /// Contacts from the previous level can linger for a moment after a level change, so they are filtered by map.
    /// </summary>
    private bool TryGetLadderOnOwnLevel(Entity<CELadderClimberComponent> ent, out Entity<CELadderComponent> ladder)
    {
        var map = Transform(ent).MapUid;
        foreach (var uid in ent.Comp.Ladders)
        {
            if (!_ladderQuery.TryComp(uid, out var comp) || Transform(uid).MapUid != map)
                continue;

            ladder = (uid, comp);
            return true;
        }

        ladder = default;
        return false;
    }

    /// <summary>
    /// True while the climber is on the level right above the ladder and still on the ladder's tile.
    /// </summary>
    private bool IsDirectlyBelow(EntityUid climber, EntityUid ladder)
    {
        if (Transform(climber).MapUid is not { } map
            || !_zLevel.TryMapDown((map, null), out var belowMap)
            || Transform(ladder).MapUid != belowMap.Owner)
            return false;

        var climberTile = Vector2Helpers.Floored(_transform.GetWorldPosition(climber));
        var ladderTile = Vector2Helpers.Floored(_transform.GetWorldPosition(ladder));
        return climberTile == ladderTile;
    }

    private void ClearBelowLadder(Entity<CELadderClimberComponent> ent)
    {
        if (ent.Comp.BelowLadder == null)
            return;

        ent.Comp.BelowLadder = null;
        Dirty(ent);

        // Called from inside the z-physics step, so the component is removed deferred.
        if (ent.Comp.Ladders.Count == 0)
            RemCompDeferred<CELadderClimberComponent>(ent);
    }

    private void TryLetGo(Entity<CELadderClimberComponent> ent)
    {
        if (ent.Comp.Ladders.Count > 0 || ent.Comp.BelowLadder != null)
            return;

        RemComp<CELadderClimberComponent>(ent);
    }

    /// <summary>
    /// World-space direction of the held movement keys. Built from the networked buttons instead of
    /// <see cref="InputMoverComponent.WishDir"/>, so other players' climbing is simulated the same way on clients.
    /// </summary>
    private Vector2 GetInputDirection(InputMoverComponent mover)
    {
        var buttons = mover.HeldMoveButtons;
        var dir = Vector2.Zero;

        if ((buttons & MoveButtons.Up) != 0)
            dir.Y += 1;
        if ((buttons & MoveButtons.Down) != 0)
            dir.Y -= 1;
        if ((buttons & MoveButtons.Right) != 0)
            dir.X += 1;
        if ((buttons & MoveButtons.Left) != 0)
            dir.X -= 1;

        if (dir == Vector2.Zero)
            return dir;

        return _mover.GetParentGridAngle(mover).RotateVec(dir.Normalized());
    }

    private float GetMoveSpeed(EntityUid uid, InputMoverComponent mover)
    {
        if (!_speedQuery.TryComp(uid, out var speed))
        {
            return mover.Sprinting
                ? MovementSpeedModifierComponent.DefaultBaseSprintSpeed
                : MovementSpeedModifierComponent.DefaultBaseWalkSpeed;
        }

        return mover.Sprinting ? speed.CurrentSprintSpeed : speed.CurrentWalkSpeed;
    }

    private void TryPlayStepSound(EntityUid climber, Entity<CELadderComponent> ladder, CEZPhysicsComponent zPhys, float heightDelta)
    {
        if (heightDelta == 0f || ladder.Comp.StepSound == null || ladder.Comp.StepInterval <= 0f)
            return;

        var before = zPhys.CurrentZLevel + zPhys.LocalPosition;
        var after = before + heightDelta;
        if (MathF.Floor(before / ladder.Comp.StepInterval) == MathF.Floor(after / ladder.Comp.StepInterval))
            return;

        // Clients simulate every body; only the local player predicts its own steps.
        if (_net.IsClient && _player.LocalEntity != climber)
            return;

        _audio.PlayPredicted(ladder.Comp.StepSound, climber, climber);
    }
}
