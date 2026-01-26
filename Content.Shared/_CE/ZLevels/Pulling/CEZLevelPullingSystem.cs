using Content.Shared._CE.ZLevels.Core.EntitySystems;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Pulling.Systems;
using Content.Shared.Pulling.Events;
using Robust.Shared.Timing;

namespace Content.Shared._CE.ZLevels.Pulling;

public sealed class CEZLevelPullingSystem : EntitySystem
{
    [Dependency] private readonly PullingSystem _pulling = default!;
    [Dependency] private readonly CESharedZLevelsSystem _zlevel = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly IGameTiming _timing = default!;


    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CEZLevelActivePullerComponent, CEZLevelMapMoveEvent>(OnZlevelMapMovePuller);
        SubscribeLocalEvent<CEZLevelActivePulledComponent, MoveEvent>(OnZlevelMapMovePulled);
        SubscribeLocalEvent<ActivePullerComponent, CETryZLevelMapMoveEvent>(OnZlevelMapMoving);
        //SubscribeLocalEvent<CEZLevelActivePullerComponent, StartPullAttemptEvent>(OnPullingStart);
    }

    private void OnZlevelMapMoving(Entity<ActivePullerComponent> pullerEnt, ref CETryZLevelMapMoveEvent args)
    {
        if (!_timing.IsFirstTimePredicted) return;

        if (args.Cancelled) return;

        if (!_pulling.IsPulling(pullerEnt)) return;

        var pulledEnt = _pulling.GetPulling(pullerEnt);
        if (!pulledEnt.HasValue) return;

        var pullerComp = EnsureComp<CEZLevelActivePullerComponent>(pullerEnt);
        var pulledComp = EnsureComp<CEZLevelActivePulledComponent>(pulledEnt.Value);

        pulledComp.PullerEnt = pullerEnt;
        pullerComp.PulledEnt = pulledEnt.Value;

        Dirty(pullerEnt, pullerComp);
        Dirty(pulledEnt.Value, pulledComp);

        _pulling.TryStopPull(pulledEnt.Value, Comp<PullableComponent>(pulledEnt.Value));
    }

    private void OnZlevelMapMovePuller(Entity<CEZLevelActivePullerComponent> pullerEnt, ref CEZLevelMapMoveEvent args)
    {
        if (!_timing.IsFirstTimePredicted) return;

        var pulledEnt = pullerEnt.Comp.PulledEnt;
        if (!TryComp<CEZLevelActivePulledComponent>(pulledEnt, out var pulledComp))
        {
            RemComp<CEZLevelActivePullerComponent>(pullerEnt);
            return;
        }
        if (pulledComp.PullerEnt != pullerEnt.Owner)
        {
            RemComp<CEZLevelActivePulledComponent>(pulledEnt);
            RemComp<CEZLevelActivePullerComponent>(pullerEnt);
            return;
        }
        if (!_zlevel.TryMove(pulledEnt, args.Offset))
        {
            RemComp<CEZLevelActivePulledComponent>(pulledEnt);
            RemComp<CEZLevelActivePullerComponent>(pullerEnt);
            return;
        }
        _transform.SetCoordinates(pulledEnt, Transform(pullerEnt).Coordinates);


        RemComp<CEZLevelActivePulledComponent>(pulledEnt);
        RemComp<CEZLevelActivePullerComponent>(pullerEnt);
    }
    private void OnZlevelMapMovePulled(Entity<CEZLevelActivePulledComponent> pulledEnt, ref MoveEvent args)
    {
        if (!_timing.IsFirstTimePredicted) return;

        var pullerEnt = pulledEnt.Comp.PullerEnt;
        if (!TryComp<CEZLevelActivePullerComponent>(pullerEnt, out var pullerComp))
        {
            RemComp<CEZLevelActivePulledComponent>(pulledEnt);
            return;
        }
        if (pullerComp.PulledEnt != pulledEnt.Owner)
        {
            RemComp<CEZLevelActivePulledComponent>(pulledEnt);
            RemComp<CEZLevelActivePullerComponent>(pullerEnt);
            return;
        }


        if (!_pulling.CanPull(pullerEnt, pulledEnt))
        {
            RemComp<CEZLevelActivePulledComponent>(pulledEnt);
            return;
        }
        _pulling.TryStartPull(pullerEnt, pulledEnt);

        RemComp<CEZLevelActivePulledComponent>(pulledEnt);
        RemComp<CEZLevelActivePullerComponent>(pulledEnt);
    }

    private void OnPullingStart(Entity<CEZLevelActivePullerComponent> ent, ref StartPullAttemptEvent args)
    {
        if (_pulling.IsPulling(ent))
            args.Cancel();
    }
}

