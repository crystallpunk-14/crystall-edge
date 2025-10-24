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
        if (!TryMapDown(ent, out var belowMapId, out var belowMapUid))
            return;
        if (!TryComp<MapGridComponent>(belowMapUid, out var belowMapGrid))
            return;
        if (!TryComp<MapGridComponent>(ent, out var mapGrid))
            return;

        foreach (var change in args.Changes)
        {
            //Update rooving below map
            Roof.SetRoof(belowMapUid.Value, change.GridIndices, !change.NewTile.IsEmpty);
        }
    }
}
