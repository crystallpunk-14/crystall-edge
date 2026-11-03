using Content.Shared._CE.Farming.Components;
using Content.Shared.Physics;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Farming;

public abstract partial class CESharedFarmingSystem
{
    /// <summary>
    /// The tile is a soil the plant can draw resources from, or the plant doesn't need soil.
    /// </summary>
    public bool IsSoilSuitable(EntityPrototype plant, Entity<MapGridComponent> grid, Vector2i tile)
    {
        if (!plant.TryGetComponent<CEPlantComponent>(out var plantComp, _compFactory)
            || plantComp.SoilResourceGathering.Count == 0)
            return true;

        var tileRef = MapSystem.GetTileRef(grid, grid.Comp, tile);
        return plantComp.SoilResourceGathering.ContainsKey(Turf.GetContentTileDefinition(tileRef));
    }

    /// <summary>
    /// Another plant or a hard <see cref="CollisionGroup.MobMask"/> body takes the tile.
    /// </summary>
    public bool IsTileOccupied(Entity<MapGridComponent> grid, Vector2i tile, EntityUid? exclude = null)
    {
        var enumerator = MapSystem.GetAnchoredEntitiesEnumerator(grid, grid.Comp, tile);
        while (enumerator.MoveNext(out var anchored))
        {
            if (anchored == exclude)
                continue;

            if (PlantQuery.HasComp(anchored))
                return true;

            if (!_physicsQuery.TryGetComponent(anchored, out var body) || !body.CanCollide || !body.Hard)
                continue;

            if ((body.CollisionLayer & (int) CollisionGroup.MobMask) != 0)
                return true;
        }

        return false;
    }

    protected bool CanPlant(EntProtoId plant, EntityCoordinates position, EntityUid? exclude = null)
    {
        if (!_proto.Resolve(plant, out var plantProto))
            return false;

        var map = _transform.GetMap(position);
        if (!TryComp<MapGridComponent>(map, out var gridComp))
            return false;

        var grid = new Entity<MapGridComponent>(map.Value, gridComp);
        var tile = MapSystem.TileIndicesFor(grid, position);
        return IsSoilSuitable(plantProto, grid, tile) && !IsTileOccupied(grid, tile, exclude);
    }
}
