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
        if (TryMapDown(ent, out _, out var belowMapUid))
        {
            SyncMapRoofs(belowMapUid.Value, ent); //Sync for map below
        }

        if (TryMapDown(ent, out _, out var aboveMapUid))
        {
            SyncMapRoofs(ent, aboveMapUid); //Sync for this map
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
        var counter = 0;
        while (enumerator.MoveNext(out var tileRef))
        {
            counter++;
            Roof.SetRoof((currentMapUid, currentMapGrid, currentRoof), tileRef.Value.GridIndices, !tileRef.Value.Tile.IsEmpty);
        }
    }
}
