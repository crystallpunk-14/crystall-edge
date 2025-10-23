using Content.Shared.Light.EntitySystems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Shared._CE.ZLevels.EntitySystems;

public abstract partial class CESharedZLevelsSystem
{
    [Dependency] protected readonly SharedRoofSystem Roof = default!;
    [Dependency] private readonly ITileDefinitionManager _tile = default!;
    private void InitRoof()
    {
        SubscribeLocalEvent<CEZLevelMapComponent, TileChangedEvent>(OnTileChanged);
        SubscribeLocalEvent<CEZLevelRoofPlacerComponent, MapInitEvent>(OnRoofPlacerMapInit);
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

            //Ensure tile above RoofPlacer
            if (change.NewTile.IsEmpty)
            {
                var anchoredEntitiesBelow = _map.GetAnchoredEntitiesEnumerator(belowMapUid.Value, belowMapGrid, change.GridIndices);
                while (anchoredEntitiesBelow.MoveNext(out var anchoredUid))
                {
                    if (!TryComp<CEZLevelRoofPlacerComponent>(anchoredUid, out var roofPlacer))
                        continue;

                    _map.SetTile((ent.Owner, mapGrid), change.GridIndices, new Tile(Proto.Index(roofPlacer.Tile).TileId));
                }
            }
        }
    }

    private void OnRoofPlacerMapInit(Entity<CEZLevelRoofPlacerComponent> ent, ref MapInitEvent args)
    {
        RoomPlacerProcess(ent);
    }

    protected void RoomPlacerProcess(Entity<CEZLevelRoofPlacerComponent> ent)
    {
        var placerXform = Transform(ent);

        if (placerXform.MapUid is null)
            return;
        if (!TryMapUp(placerXform.MapUid.Value, out var aboveMapId, out var aboveMapUid))
            return;
        if (!TryComp<MapGridComponent>(aboveMapUid, out var aboveMapGrid))
            return;

        var indices = _map.CoordinatesToTile(aboveMapUid.Value,
            aboveMapGrid,
            new MapCoordinates(_transform.GetWorldPosition(ent), aboveMapId.Value));

        if (_map.TryGetTileRef(aboveMapUid.Value, aboveMapGrid, indices, out var tileRef))
        {
            if (!tileRef.Tile.IsEmpty)
                return;
        }

        _map.SetTile((aboveMapUid.Value, aboveMapGrid), indices, new Tile(Proto.Index(ent.Comp.Tile).TileId));
    }
}
