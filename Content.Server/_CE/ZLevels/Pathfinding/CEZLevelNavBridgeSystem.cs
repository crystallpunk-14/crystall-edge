using Content.Server._CE.ZLevels.Core;
using Content.Server.Construction.Completions;
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

        SubscribeLocalEvent<CEZLevelNavBridgeComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<CEZLevelNavBridgeComponent, ComponentShutdown>(OnShutdown);

        SubscribeLocalEvent<CEZLevelNavBridgeComponent, MapInitEvent>(OnMapInit, after: [typeof(CESharedZLevelsSystem)]);
    }

    private void OnMapInit(Entity<CEZLevelNavBridgeComponent> ent, ref MapInitEvent args)
    {
        if (ent.Comp.TargetMap is null && !UpdateTargetMap(ent)) return;
        if (ent.Comp.TargetEntity is null && !UpdateTargetEntity(ent)) return;

        var targetMap = ent.Comp.TargetMap;
        var targetEnt = ent.Comp.TargetEntity;

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

        var transitionPoint = Transform(ent).LocalRotation.RotateVec(ent.Comp.TransitionPoint);
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


    private void OnStartup(Entity<CEZLevelNavBridgeComponent> ent, ref ComponentStartup args)
    {
        if (ent.Comp.PortalHandels.Count > 0)
            ClearHandels(ent);
        UpdateTargetMap(ent);
        UpdateTargetEntity(ent);
    }

    private void OnShutdown(Entity<CEZLevelNavBridgeComponent> ent, ref ComponentShutdown args)
    {
        ClearHandels(ent);
    }

    private void ClearHandels(Entity<CEZLevelNavBridgeComponent> ent)
    {
        foreach (var handle in ent.Comp.PortalHandels)
        {
            _pathfinding.RemovePortal(handle.Value);
            _entity.DeleteEntity(handle.Key.EntityId);
        }
        ent.Comp.PortalHandels.Clear();
    }
}
