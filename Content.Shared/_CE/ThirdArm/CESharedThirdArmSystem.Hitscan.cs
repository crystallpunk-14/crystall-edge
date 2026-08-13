using System.Numerics;
using Content.Shared._CE.ThirdArm.Components;
using Content.Shared.Actions;
using Content.Shared.Weapons.Hitscan.Events;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared._CE.ThirdArm;

/// <summary>
///     This is pure shitcode, need to be refactored in future.
/// </summary>
public abstract partial class CESharedThirdArmSystem
{
    [Dependency] protected IGameTiming Timing = default!;
    [Dependency] protected SharedTransformSystem TransformSystem = default!;
    [Dependency] protected SharedAudioSystem Audio = default!;
    [Dependency] private INetManager _net = default!;

    private const int SalvoShotCount = 5;
    private static readonly TimeSpan SalvoShotDelay = TimeSpan.FromSeconds(0.1f);
    private const double SalvoAngleStepDegrees = 2f;

    private void InitHitscanModule()
    {
        SubscribeLocalEvent<CEThirdArmModuleComponent, CEThirdArmHitscanActionEvent>(OnHitscanAction);
    }

    private void OnHitscanAction(Entity<CEThirdArmModuleComponent> ent, ref CEThirdArmHitscanActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        ScheduleHitscanSalvo(args.Performer, args.Target, args.Entity, args.Hitscan, args.Sound);
    }

    private void ScheduleHitscanSalvo(EntityUid shooter, EntityCoordinates target, EntityUid? targetEntity, EntProtoId hitscanProto, SoundSpecifier? sound)
    {
        if (_net.IsClient)
            return;

        var salvo = EnsureComp<CEThirdArmHitscanSalvoComponent>(shooter);
        var curTime = Timing.CurTime;

        // Center shot is exactly on the cursor, the rest fan out symmetrically around it.
        var centerIndex = (SalvoShotCount - 1) / 2;

        for (var i = 0; i < SalvoShotCount; i++)
        {
            var stepsFromCenter = i - centerIndex;

            salvo.Pending.Add(new CEThirdArmScheduledHitscan
            {
                FireTime = curTime + SalvoShotDelay * i,
                AngleOffset = Angle.FromDegrees(SalvoAngleStepDegrees * stepsFromCenter),
                Shooter = shooter,
                TargetCoordinates = target,
                TargetEntity = targetEntity,
                Hitscan = hitscanProto,
                Sound = sound,
            });
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_net.IsClient)
            return;

        var curTime = Timing.CurTime;
        var query = EntityQueryEnumerator<CEThirdArmHitscanSalvoComponent>();

        while (query.MoveNext(out var uid, out var salvo))
        {
            for (var i = salvo.Pending.Count - 1; i >= 0; i--)
            {
                var shot = salvo.Pending[i];
                if (shot.FireTime > curTime)
                    continue;

                FireHitscanShot(shot);
                salvo.Pending.RemoveAt(i);
            }

            if (salvo.Pending.Count == 0)
                RemComp<CEThirdArmHitscanSalvoComponent>(uid);
        }
    }

    private void FireHitscanShot(CEThirdArmScheduledHitscan shot)
    {
        if (_net.IsClient)
            return;

        if (Deleted(shot.Shooter))
            return;

        var fromCoords = Transform(shot.Shooter).Coordinates;
        var fromMap = TransformSystem.ToMapCoordinates(fromCoords);
        var targetMap = TransformSystem.ToMapCoordinates(shot.TargetCoordinates);

        var baseDirection = targetMap.Position - fromMap.Position;
        if (baseDirection == Vector2.Zero)
            return;

        var direction = shot.AngleOffset.RotateVec(baseDirection).Normalized();

        Audio.PlayPvs(shot.Sound, fromCoords);

        var hitscanEnt = Spawn(shot.Hitscan, fromCoords);

        var hitscanEv = new HitscanTraceEvent
        {
            FromCoordinates = fromCoords,
            ShotDirection = direction,
            Gun = shot.Shooter,
            Shooter = shot.Shooter,
            Target = shot.TargetEntity,
        };
        RaiseLocalEvent(hitscanEnt, ref hitscanEv);

        Del(hitscanEnt);
    }
}

/// <summary>
///     Fires a "cone" of hitscan shots from the performer towards the clicked target. Raised on the granting
///     module (its container/provider), per the default action-event routing (see
///     SharedActionsSystem.PerformAction). Mana cost, if any, comes from a separate
///     CEThirdArmActionManaCostComponent on the action entity.
/// </summary>
public sealed partial class CEThirdArmHitscanActionEvent : WorldTargetActionEvent
{
    [DataField(required: true)]
    public EntProtoId Hitscan;

    [DataField]
    public SoundSpecifier? Sound;
}
