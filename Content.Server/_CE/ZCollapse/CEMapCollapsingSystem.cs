using Content.Server._CE.ZLevels.EntitySystems;
using Content.Shared.Maps;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Server._CE.ZCollapse;

public sealed class CEMapCollapsingSystem : EntitySystem
{
    [Dependency] private readonly CEZLevelsSystem _zLevel = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly ITileDefinitionManager _tileDef = default!;

    private EntityQuery<CEMapCollapsingComponent> _collapsingQuery;
    private EntityQuery<CEMapSupportComponent> _supportQuery;
    private EntityQuery<MapGridComponent> _mapGridQuery;

    public override void Initialize()
    {
        base.Initialize();

        _supportQuery = GetEntityQuery<CEMapSupportComponent>();
        _collapsingQuery = GetEntityQuery<CEMapCollapsingComponent>();
        _mapGridQuery = GetEntityQuery<MapGridComponent>();

        SubscribeLocalEvent<CEMapCollapsingComponent, TileChangedEvent>(OnTileChanged);
        SubscribeLocalEvent<CEMapSupportComponent, ComponentInit>(OnSupportAdded);
        SubscribeLocalEvent<CEMapSupportComponent, ComponentShutdown>(OnSupportShutdown);
    }

    private void OnTileChanged(Entity<CEMapCollapsingComponent> ent, ref TileChangedEvent args)
    {
        if (!_mapGridQuery.TryComp(ent, out var mapGrid))
            return;
        _zLevel.TryMapDown(ent.Owner, out var belowMapUid); //TODO: recalculate even without map down
        if(!_mapGridQuery.TryComp(belowMapUid, out var belowMapGrid))
            return;

        foreach (var entry in args.Changes)
        {
            RecalculateSupport(ent, mapGrid, belowMapUid.HasValue ? (belowMapUid.Value, belowMapGrid) : null, entry.GridIndices);
        }
    }

    private void OnSupportShutdown(Entity<CEMapSupportComponent> ent, ref ComponentShutdown args)
    {
        if (!_zLevel.TryMapUp(ent.Owner, out var aboveMapUid))
            return;

        if (!_collapsingQuery.TryComp(aboveMapUid, out var collapsingComp))
            return;

        if (!_transform.TryGetGridTilePosition(ent.Owner, out var indices))
            return;

        if (!_mapGridQuery.TryComp(ent, out var mapGrid))
            return;
        if (!_mapGridQuery.TryComp(aboveMapUid, out var aboveMapGrid))
            return;

        RecalculateSupport((aboveMapUid.Value, collapsingComp), aboveMapGrid, (ent, mapGrid), indices);
    }

    private void OnSupportAdded(Entity<CEMapSupportComponent> ent, ref ComponentInit args)
    {
        if (!_zLevel.TryMapUp(ent.Owner, out var aboveMapUid))
            return;

        if (!_collapsingQuery.TryComp(aboveMapUid, out var collapsingComp))
            return;

        if (!_transform.TryGetGridTilePosition(ent.Owner, out var indices))
            return;

        if (!_mapGridQuery.TryComp(ent, out var mapGrid))
            return;
        if (!_mapGridQuery.TryComp(aboveMapUid, out var aboveMapGrid))
            return;

        RecalculateSupport((aboveMapUid.Value, collapsingComp), aboveMapGrid, (ent, mapGrid), indices);
    }

    private void RecalculateSupport(Entity<CEMapCollapsingComponent> ent, MapGridComponent currentMapGrid, Entity<MapGridComponent>? belowMap, Vector2i tilePos)
    {
        if (!_mapGridQuery.TryComp(ent.Owner, out var mapGrid))
        {
            ent.Comp.CollapeTileDict.Remove(tilePos);
            return;
        }

        if (!_map.TryGetTileDef(mapGrid, tilePos, out var tileDef))
        {
            ent.Comp.CollapeTileDict.Remove(tilePos);
            return;
        }

        if (belowMap is null)
        {
            ent.Comp.CollapeTileDict[tilePos] = 0;
            return;
        }

        var enumerator = _map.GetAnchoredEntitiesEnumerator(belowMap.Value, belowMap.Value.Comp, tilePos);
        while (enumerator.MoveNext(out var anchored))
        {

        }

        var tile = (ContentTileDefinition)tileDef;

        ent.Comp.CollapeTileDict[tilePos] = 1;
    }
}
