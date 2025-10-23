using Content.Shared._CE.ZLevels;
using Content.Shared.Light.Components;
using Content.Shared.Light.EntitySystems;
using Robust.Shared.Map.Components;

namespace Content.Server._CE.ZLevels.EntitySystems;

public sealed partial class CEZLevelsSystem
{
    [Dependency] private readonly SharedRoofSystem _roof = default!;
    private void InitRoofs()
    {
        SubscribeLocalEvent<MapComponent, CEMapAddedIntoZNetwork>(OnMapAdded);
        SubscribeLocalEvent<CEZLevelMapComponent, TileChangedEvent>(OnTileChanged);
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
            _roof.SetRoof((currentMapUid, currentMapGrid, currentRoof), tileRef.Value.GridIndices, !tileRef.Value.Tile.IsEmpty);
        }
    }

    private void OnTileChanged(Entity<CEZLevelMapComponent> ent, ref TileChangedEvent args)
    {
        if (!TryMapDown(ent, out var mapId, out var belowMapUid))
            return;

        foreach (var change in args.Changes)
        {
            _roof.SetRoof(belowMapUid.Value, change.GridIndices, !change.NewTile.IsEmpty);
        }
    }
}
