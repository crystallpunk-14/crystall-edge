using Content.Server._CE.ZLevels.EntitySystems;
using Content.Shared._CE.ZLevels;
using Content.Shared._CE.ZRoof;
using Content.Shared.Light.Components;
using Robust.Server.GameObjects;

namespace Content.Server._CE.ZRoof;

/// <inheritdoc/>
public sealed class CERoofSystem : CESharedRoofSystem
{
    [Dependency] private readonly MapSystem _map = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CEZLevelMapComponent, CEMapAddedIntoZNetwork>(OnMapAdded);
    }

    private void OnMapAdded(Entity<CEZLevelMapComponent> ent, ref CEMapAddedIntoZNetwork args)
    {
        //Sync for map below
        if (ZLevel.TryMapDown((ent.Owner, ent.Comp), out var belowMapUid))
            SyncMapRoofs(belowMapUid.Value, ent);

        //Sync for this map
        if (ZLevel.TryMapUp((ent.Owner, ent.Comp), out var aboveMapUid))
            SyncMapRoofs(ent, aboveMapUid.Value);
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
