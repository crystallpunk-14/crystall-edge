using Content.Server._CE.ZLevels.Core;
using Content.Server.NPC.Pathfinding;
using Content.Shared._CE.ZLevels.Core.Components;
using Content.Shared._CE.ZLevels.Core.EntitySystems;
using Content.Shared.Coordinates;
using Robust.Server.GameObjects;
using Robust.Shared.Map;

namespace Content.Server._CE.ZLevels.Pathfinding;

public sealed partial class CEZLevelNavBridgeSystem : EntitySystem
{
    [Dependency] private readonly PathfindingSystem _pathfinding = default!;
    [Dependency] private readonly EntityManager _entity = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly CEZLevelsSystem _zLevel = default!;
    [Dependency] private readonly TransformSystem _transform = default!;



    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CEZLevelNavBridgeComponent, ComponentStartup>(OnInit);
        SubscribeLocalEvent<CEZLevelNavBridgeComponent, MapInitEvent>(OnMapInit, after: [typeof(CESharedZLevelsSystem)]);
    }

    private void OnMapInit(Entity<CEZLevelNavBridgeComponent> ent, ref MapInitEvent args)
    {
        var targetMap = ent.Comp.TargetMap;
        if (targetMap is null) return;
        if (!_map.TryGetMap(targetMap, out var targetMapEnt)) return;
        if (!HasComp<CEZLevelMapComponent>(targetMapEnt)) return;
        if (ent.Comp.TargetEntity is null) return;

        EntityCoordinates transitionPoint1 = new(ent.Owner, ent.Comp.TransitionPoint);
        EntityCoordinates transitionPoint2 = new(ent.Comp.TargetEntity.Value, ent.Comp.TransitionPoint);

        _pathfinding.TryCreatePortal(transitionPoint1, transitionPoint2, out var handle);

        ent.Comp.PortalHandels.Add(transitionPoint2, handle);
    }


    private void OnInit(Entity<CEZLevelNavBridgeComponent> ent, ref ComponentStartup args)
    {
        var entMapid = Transform(ent).MapID;

        if (!_map.TryGetMap(entMapid, out var entMapEnt)) return;
        if (!HasComp<CEZLevelMapComponent>(entMapEnt)) return;
        if (!_zLevel.TryMapUp(entMapEnt.Value, out var newMapEnt)) return;

        ent.Comp.TargetMap ??= Transform(newMapEnt.Value).MapID;

        var targetMap = ent.Comp.TargetMap;

        if (!_map.TryGetMap(targetMap, out var targetMapEnt)) return;
        if (!HasComp<CEZLevelMapComponent>(targetMapEnt)) return;

        var transitionPoint = ent.Comp.TransitionPoint;
        var mapTransitionPoint = _transform.ToMapCoordinates(new EntityCoordinates(ent, transitionPoint));
        var targetEnt = _entity.Spawn(null, new MapCoordinates(mapTransitionPoint.Position, targetMap.Value));

        ent.Comp.TargetEntity = targetEnt;
    }
}
