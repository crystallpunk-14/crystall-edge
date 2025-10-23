using Content.Shared._CE.ZLevels;
using Content.Shared.Light.Components;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Server._CE.ZLevels.EntitySystems;

public sealed partial class CEZLevelsSystem
{
    private void InitRoofs()
    {
        SubscribeLocalEvent<MapComponent, CEMapAddedIntoZNetwork>(OnMapAdded);
    }

    private void OnMapAdded(Entity<MapComponent> ent, ref CEMapAddedIntoZNetwork args)
    {
        if (TryMapDown(ent, out _, out var belowMapUid))
        {
            //Sync for map below
            SyncMapRoofs(belowMapUid.Value, ent);
            SyncMapTiles(ent, belowMapUid);
        }

        if (TryMapUp(ent, out _, out var aboveMapUid))
        {
            //Sync for this map
            SyncMapRoofs(ent, aboveMapUid);
            SyncMapTiles(aboveMapUid.Value, ent);
        }
    }

    /// <summary>
    /// Go through all the tiles on the map above, synchronizing the roofs on this map.
    /// </summary>
    private void SyncMapRoofs(EntityUid currentMapUid, EntityUid? aboveMapUid = null)
    {
        if (!TryComp<MapGridComponent>(currentMapUid, out var currentMapGrid))
            return;

        if (aboveMapUid is null && !TryMapUp(currentMapUid, out _, out aboveMapUid))
            return;

        if (!TryComp<MapGridComponent>(aboveMapUid, out var aboveMapGrid))
            return;

        var enumerator = _map.GetAllTilesEnumerator(aboveMapUid.Value, aboveMapGrid);
        var currentRoof = EnsureComp<RoofComponent>(currentMapUid);
        while (enumerator.MoveNext(out var tileRef))
        {
            Roof.SetRoof((currentMapUid, currentMapGrid, currentRoof), tileRef.Value.GridIndices, !tileRef.Value.Tile.IsEmpty);
        }
    }

    /// <summary>
    /// Goes through all RoofPlacer on the map from the bottom and places tiles on this map if there are empty tiles.
    /// </summary>
    private void SyncMapTiles(EntityUid currentMapUid, EntityUid? belowMapUid = null)
    {
        if (!TryComp<MapGridComponent>(currentMapUid, out var currentMapGrid))
            return;

        if (belowMapUid is null && !TryMapDown(currentMapUid, out var belowMapId, out belowMapUid))
            return;

        if (!TryComp<MapGridComponent>(belowMapUid, out var belowMapGrid))
            return;

        var query = EntityQueryEnumerator<CEZLevelRoofPlacerComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var roofPlacer, out var xform))
        {
            if (xform.MapUid != belowMapUid)
                continue;

            var indices = _map.CoordinatesToTile(currentMapUid,
                currentMapGrid,
                new MapCoordinates(_transform.GetWorldPosition(uid), _transform.GetMapId(currentMapUid)));

            if (_map.TryGetTileRef(currentMapUid, currentMapGrid, indices, out var tileRef))
            {
                if (!tileRef.Tile.IsEmpty)
                    return;
            }

            _map.SetTile((currentMapUid, currentMapGrid), indices, new Tile(Proto.Index(roofPlacer.Tile).TileId));
        }
    }
}
