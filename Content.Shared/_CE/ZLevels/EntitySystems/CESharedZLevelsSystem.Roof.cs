using Content.Shared.Light.EntitySystems;
using Robust.Shared.Map.Components;

namespace Content.Shared._CE.ZLevels.EntitySystems;

public abstract partial class CESharedZLevelsSystem
{
    [Dependency] protected readonly SharedRoofSystem Roof = default!;
    private void InitRoof()
    {
        SubscribeLocalEvent<CEZLevelMapComponent, TileChangedEvent>(OnTileChanged);
    }

    private void OnTileChanged(Entity<CEZLevelMapComponent> ent, ref TileChangedEvent args)
    {
        if (!_mapQuery.TryComp(ent, out var currentMap))
            return;
        if (!TryMapDown(currentMap.MapId, out _, out var belowMapUid))
            return;

        foreach (var change in args.Changes)
        {
            //Update rooving below map
            Roof.SetRoof(belowMapUid.Value.Owner, change.GridIndices, !change.NewTile.IsEmpty);
        }
    }
}
