using Content.Shared._CE.ZLevels.Core.EntitySystems;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Pulling.Systems;

namespace Content.Shared._CE.ZLevels.Pulling;

public sealed class CEZLevelPullingSystem : EntitySystem
{
    [Dependency] private readonly PullingSystem _pulling = default!;
    [Dependency] private readonly CESharedZLevelsSystem _zlevel = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CEZLevelActivePullerComponent, CEZLevelMapMoveEvent>(OnZlevelMapMovePuller);
        SubscribeLocalEvent<CEZLevelActivePulledComponent, MoveEvent>(OnZlevelMapMovePulled);
        SubscribeLocalEvent<ActivePullerComponent, CETryZLevelMapMoveEvent>(OnZlevelMapMoving);
    }

    private void OnZlevelMapMoving(Entity<ActivePullerComponent> ent, ref CETryZLevelMapMoveEvent args)
    {
        if (args.Cancelled) return;

        if (!_pulling.IsPulling(ent)) return;

        var pulledEnt = _pulling.GetPulling(ent);
        if (!pulledEnt.HasValue) return;

        AddComp<CEZLevelActivePullerComponent>(ent, new() { PulledEnt = pulledEnt.Value });
        AddComp<CEZLevelActivePulledComponent>(pulledEnt.Value, new() { PullerEnt = ent });

        _pulling.TryStopPull(pulledEnt.Value, Comp<PullableComponent>(pulledEnt.Value));
    }

    private void OnZlevelMapMovePuller(Entity<CEZLevelActivePullerComponent> ent, ref CEZLevelMapMoveEvent args)
    {
        var pulledEnt = ent.Comp.PulledEnt;
        if (!TryComp<CEZLevelActivePulledComponent>(pulledEnt, out var pulledComp))
        {
            RemComp<CEZLevelActivePullerComponent>(ent);
            return;
        }
        if (pulledComp.PullerEnt != ent.Owner)
        {
            RemComp<CEZLevelActivePulledComponent>(pulledEnt);
            RemComp<CEZLevelActivePullerComponent>(ent);
            return;
        }
        if (!_zlevel.TryMove(pulledEnt, args.Offset))
        {
            RemComp<CEZLevelActivePulledComponent>(pulledEnt);
            RemComp<CEZLevelActivePullerComponent>(ent);
            return;
        }
        _transform.SetCoordinates(pulledEnt, Transform(ent).Coordinates);


        RemComp<CEZLevelActivePullerComponent>(ent);

    }
    private void OnZlevelMapMovePulled(Entity<CEZLevelActivePulledComponent> ent, ref MoveEvent args)
    {
        var pullerEnt = ent.Comp.PullerEnt;
        if (!TryComp<CEZLevelActivePullerComponent>(pullerEnt, out var pullerComp))
        {
            RemComp<CEZLevelActivePulledComponent>(ent);
            return;
        }
        if (pullerComp.PulledEnt != ent.Owner)
        {
            RemComp<CEZLevelActivePulledComponent>(ent);
            RemComp<CEZLevelActivePullerComponent>(pullerEnt);
            return;
        }


        if (!_pulling.CanPull(pullerEnt, ent))
        {
            RemComp<CEZLevelActivePulledComponent>(ent);
            return;
        }

        _pulling.TryStartPull(pullerEnt, ent);

        RemComp<CEZLevelActivePulledComponent>(ent);
    }
}

