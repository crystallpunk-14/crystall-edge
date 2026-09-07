using System.Numerics;
using Content.Server._CE.MeleeWeapon;
using Content.Server._CE.GOAP.Classifiers;
using Content.Server._CE.NPC;
using Content.Server.NPC.Systems;
using Content.Shared._CE.Animation.Item.Components;
using Content.Shared._CE.GOAP;
using Content.Shared._CE.GOAP.Components;
using Content.Shared._CE.MeleeWeapon;
using Content.Shared.CombatMode;
using Content.Shared.Movement.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Timing;

namespace Content.Server._CE.GOAP.Actions;

/// <summary>Restricts autonomous melee hits to known threats, including animations finishing after an action.</summary>
[RegisterComponent]
public sealed partial class CEGOAPThreatOnlyMeleeComponent : Component
{
}

/// <summary>Approaches and attacks a threat while moving around it at melee distance.</summary>
public sealed partial class CEGOAPCircleMeleeAction : CEGOAPActionBase<CEGOAPCircleMeleeAction>
{
    [DataField] public float OrbitRadius = 0.8f;
    [DataField] public float AttackRange = 1f;
    [DataField] public float PursuitRange = 10f;
    [DataField] public TimeSpan RepathInterval = TimeSpan.FromSeconds(0.4);
}

[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class CEGOAPCircleMeleeComponent : Component
{
    [DataField] public bool AddedRotationLock;
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextRepath;
}

public sealed partial class CEGOAPCircleMeleeActionSystem : CEGOAPActionSystem<CEGOAPCircleMeleeAction>
{
    [Dependency] private NPCSteeringSystem _steering = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private CEWeaponSystem _weapon = default!;
    [Dependency] private SharedCombatModeSystem _combat = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CEGOAPThreatOnlyMeleeComponent, CEWeaponArcTargetsEvent>(OnArcTargets);
    }

    private void OnArcTargets(Entity<CEGOAPThreatOnlyMeleeComponent> ent, ref CEWeaponArcTargetsEvent args)
    {
        if (HasComp<ActorComponent>(ent))
            return;

        // A defensive swing must not turn nearby allies into retaliating attackers.
        if (!TryComp<CEGOAPKnowledgeCacheComponent>(ent, out var knowledge))
        {
            args.Targets.Clear();
            return;
        }
        args.Targets.RemoveAll(target => !knowledge.Enemies.Contains(target));
    }

    protected override void OnActionStartup(Entity<CEGOAPComponent> ent, ref CEGOAPActionStartupEvent<CEGOAPCircleMeleeAction> args)
    {
        var state = EnsureComp<CEGOAPCircleMeleeComponent>(ent);
        state.NextRepath = TimeSpan.Zero;
        // Orbit movement must not turn the weapon away from its target during the attack animation.
        state.AddedRotationLock = !HasComp<NoRotateOnMoveComponent>(ent);
        EnsureComp<NoRotateOnMoveComponent>(ent);
        _combat.SetInCombatMode(ent, true);
        if (TryComp<CENPCMovementComponent>(ent, out var movement))
            movement.Walking = false;
    }

    protected override void OnActionUpdate(Entity<CEGOAPComponent> ent, ref CEGOAPActionUpdateEvent<CEGOAPCircleMeleeAction> args)
    {
        var result = args.Action.Selector?.Resolve(ent, EntityManager);
        if (result?.Entity is not { } target || !Exists(target) ||
            TryComp<MobStateComponent>(target, out var mob) && _mobState.IsIncapacitated(target, mob) ||
            !Transform(ent).Coordinates.TryDistance(EntityManager, Transform(target).Coordinates, out var distance) ||
            distance > args.Action.PursuitRange)
        {
            args.Status = CEGOAPActionStatus.Finished;
            return;
        }

        if (!_weapon.TryGetWeapon(ent, out var weapon))
        {
            args.Status = CEGOAPActionStatus.Failed;
            return;
        }

        var origin = _transform.GetWorldPosition(ent);
        var targetXform = Transform(target);
        var targetPosition = _transform.GetWorldPosition(targetXform);
        var towards = targetPosition - origin;
        if (distance <= args.Action.AttackRange)
            _weapon.TryUse(ent, weapon.Value, CEUseType.Primary, Angle.FromWorldVec(towards));

        var state = Comp<CEGOAPCircleMeleeComponent>(ent);
        if (_timing.CurTime < state.NextRepath)
            return;
        state.NextRepath = _timing.CurTime + args.Action.RepathInterval;

        EntityCoordinates destination;
        if (distance > args.Action.OrbitRadius + 0.8f)
        {
            destination = targetXform.Coordinates;
        }
        else
        {
            var radial = origin - targetPosition;
            radial = radial.LengthSquared() > 0.001f ? Vector2.Normalize(radial) : Vector2.UnitX;
            var turn = new Angle(ent.Owner.Id % 2 == 0 ? 0.8 : -0.8);
            var position = targetPosition + turn.RotateVec(radial) * args.Action.OrbitRadius;
            var local = Vector2.Transform(position, _transform.GetInvWorldMatrix(targetXform.ParentUid));
            destination = new EntityCoordinates(targetXform.ParentUid, local);
        }

        _steering.Unregister(ent);
        _steering.Register(ent, destination).Range = 0.15f;
    }

    protected override void OnActionShutdown(Entity<CEGOAPComponent> ent, ref CEGOAPActionShutdownEvent<CEGOAPCircleMeleeAction> args)
    {
        _steering.Unregister(ent);
        _combat.SetInCombatMode(ent, false);
        if (TryComp<CENPCMovementComponent>(ent, out var movement))
            movement.Walking = true;
        if (TryComp<CEGOAPCircleMeleeComponent>(ent, out var state) && state.AddedRotationLock)
            RemComp<NoRotateOnMoveComponent>(ent);
        RemComp<CEGOAPCircleMeleeComponent>(ent);
    }
}
