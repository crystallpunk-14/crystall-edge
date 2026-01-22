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

        SubscribeLocalEvent<CEZLevelActivePullerComponent, CEZLevelMapMoveEvent>(OnZlevelMapMove);
        SubscribeLocalEvent<ActivePullerComponent, CETryZLevelMapMoveEvent>(OnZlevelMapMoving);
    }

    private void OnZlevelMapMoving(Entity<ActivePullerComponent> ent, ref CETryZLevelMapMoveEvent args)
    {
        if (args.Cancelled) return;

        if (!_pulling.IsPulling(ent)) return;

        var pulledEnt = _pulling.GetPulling(ent);
        if (pulledEnt is null) return;

        AddComp<CEZLevelActivePullerComponent>(ent, new() { PulledEnt = pulledEnt.Value });
    }

    private void OnZlevelMapMove(Entity<CEZLevelActivePullerComponent> ent, ref CEZLevelMapMoveEvent args)
    {
        var pulledEnt = ent.Comp.PulledEnt;
        if (!_zlevel.TryMove(pulledEnt, args.Offset))
        {
            RemComp<CEZLevelActivePullerComponent>(ent);
            return;
        }

        _transform.SetCoordinates(pulledEnt, Transform(ent).Coordinates);
        _pulling.TryStartPull(ent, pulledEnt);

        RemComp<CEZLevelActivePullerComponent>(ent);
    }
}

