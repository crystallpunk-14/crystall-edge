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
        var targetEnt = ent.Comp.TargetEntity;

        if (targetMap is null && !UpdateTargetMap(ent)) return;
        if (targetEnt is null && !UpdateTargetEntity(ent)) return;

        if (!_map.TryGetMap(targetMap, out var targetMapEnt)) return;
        if (!HasComp<CEZLevelMapComponent>(targetMapEnt)) return;

        EntityCoordinates transitionPoint1 = new(ent.Owner, ent.Comp.TransitionPoint);
        EntityCoordinates transitionPoint2 = new(targetEnt!.Value, ent.Comp.TransitionPoint);

        if (!_pathfinding.TryCreatePortal(transitionPoint1, transitionPoint2, out var handle)) return;

        ent.Comp.PortalHandels.Add(transitionPoint2, handle);
    }
    public bool UpdateTargetEntity(Entity<CEZLevelNavBridgeComponent> ent)
    {
        var targetMap = ent.Comp.TargetMap;

        if (!_map.TryGetMap(targetMap, out var targetMapEnt)) return false;
        if (!HasComp<CEZLevelMapComponent>(targetMapEnt)) return false;

        var transitionPoint = ent.Comp.TransitionPoint;
        var mapTransitionPoint = _transform.ToMapCoordinates(new EntityCoordinates(ent, transitionPoint));
        var targetEnt = _entity.Spawn(null, new MapCoordinates(mapTransitionPoint.Position, targetMap.Value));

        ent.Comp.TargetEntity = targetEnt;
        return true;
    }

    public bool UpdateTargetMap(Entity<CEZLevelNavBridgeComponent> ent)
    {
        var entMapid = Transform(ent).MapID;

        if (!_map.TryGetMap(entMapid, out var entMapEnt)) return false;
        if (!_zLevel.TryMapUp(entMapEnt.Value, out var newMapEnt)) return false;
        if (!HasComp<CEZLevelMapComponent>(entMapEnt)) return false;

        ent.Comp.TargetMap = Transform(newMapEnt.Value).MapID;
        return true;
    }


    private void OnInit(Entity<CEZLevelNavBridgeComponent> ent, ref ComponentStartup args)
    {
        UpdateTargetMap(ent);
        UpdateTargetEntity(ent);
    }
}
