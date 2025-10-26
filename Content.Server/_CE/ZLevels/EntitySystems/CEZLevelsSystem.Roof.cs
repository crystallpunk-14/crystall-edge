using Content.Shared.Light.Components;
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
        if (TryMapDown(ent.Comp.MapId, out _, out var belowMapUid))
        {
            //Sync for map below
            SyncMapRoofs(belowMapUid.Value, ent);
        }

        if (TryMapUp(ent.Comp.MapId, out _, out var aboveMapUid))
        {
            //Sync for this map
            SyncMapRoofs(ent, aboveMapUid);
        }
    }

    /// <summary>
    /// Go through all the tiles on the map above, synchronizing the roofs on this map.
    /// </summary>
    private void SyncMapRoofs(Entity<MapComponent> currentMap, Entity<MapComponent>? aboveMapUid = null)
    {
        if (!TryComp<MapGridComponent>(currentMap, out var currentMapGrid))
            return;

        if (aboveMapUid is null && !TryMapUp(currentMap.Comp.MapId, out _, out aboveMapUid))
            return;

        if (!TryComp<MapGridComponent>(aboveMapUid, out var aboveMapGrid))
            return;

        var enumerator = _map.GetAllTilesEnumerator(aboveMapUid.Value, aboveMapGrid);
        var currentRoof = EnsureComp<RoofComponent>(currentMap);
        while (enumerator.MoveNext(out var tileRef))
        {
            Roof.SetRoof((currentMap, currentMapGrid, currentRoof), tileRef.Value.GridIndices, !tileRef.Value.Tile.IsEmpty);
        }
    }
}
