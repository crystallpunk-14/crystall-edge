using Content.Shared._CE.Farming.Components;
using Content.Shared._CE.Placeable;
using Content.Shared.Construction.EntitySystems;
using Content.Shared.Examine;
using Content.Shared.Physics;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Farming;

public abstract partial class CESharedFarmingSystem
{
    [SubscribeLocalEvent]
    private void OnSeedExamine(Entity<CESeedComponent> ent, ref ExaminedEvent args)
    {
        if (!TryComp<CEPlaceableComponent>(ent, out var placeable) || !_proto.Resolve(placeable.Prototype, out var plantProto))
            return;

        args.PushMarkup(Loc.GetString("ce-farming-seed-examine", ("name", plantProto.Name)));
    }

    /// <summary>
    /// The tile is a soil the plant can draw resources from. Plants that don't gather from soil grow anywhere.
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
    /// Mirrors <see cref="AnchorableSystem.TileFree(Entity{MapGridComponent}, Vector2i, int, int)"/> with MobMask,
    /// but supports exclude and treats any other plant as an obstacle. Puddles, decals and cables don't block planting.
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
