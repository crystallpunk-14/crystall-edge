using Content.Shared._CE.ZLevels;
using Content.Shared.Light.Components;

namespace Content.Server._CE.ZLevels.EntitySystems;

public sealed partial class CEZLevelsSystem
{
    private void InitRoofs()
    {
        SubscribeLocalEvent<CEZLevelMapComponent, CEMapAddedIntoZNetwork>(OnMapAdded);
    }

    private void OnMapAdded(Entity<CEZLevelMapComponent> ent, ref CEMapAddedIntoZNetwork args)
    {
        if (TryMapDown((ent.Owner, ent.Comp), out var belowMapUid))
        {
            //Sync for map below
            SyncMapRoofs(belowMapUid.Value, ent);
        }

        if (TryMapUp((ent.Owner, ent.Comp), out var aboveMapUid))
        {
            //Sync for this map
            SyncMapRoofs(ent, aboveMapUid.Value);
        }
    }

    /// <summary>
    /// Go through all the tiles on the map above, synchronizing the roofs on this map.
    /// </summary>
    private void SyncMapRoofs(Entity<CEZLevelMapComponent> currentMapUid, Entity<CEZLevelMapComponent> aboveMapUid)
    {
        if (!GridQuery.TryComp(currentMapUid, out var currentMapGrid))
            return;

        if (!GridQuery.TryComp(aboveMapUid, out var aboveMapGrid))
            return;

        var enumerator = _map.GetAllTilesEnumerator(aboveMapUid, aboveMapGrid);
        var currentRoof = EnsureComp<RoofComponent>(currentMapUid);
        while (enumerator.MoveNext(out var tileRef))
        {
            Roof.SetRoof((currentMapUid, currentMapGrid, currentRoof), tileRef.Value.GridIndices, !tileRef.Value.Tile.IsEmpty);
        }
    }
}
