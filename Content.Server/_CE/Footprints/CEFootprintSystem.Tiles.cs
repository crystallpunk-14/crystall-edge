using System.Numerics;
using Content.Shared._CE.Footprints;
using Content.Shared.Maps;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Spawners;

namespace Content.Server._CE.Footprints;

/// <summary>
/// Temporary prints pressed into soft tiles (snow, sand) by clean feet.
/// </summary>
public sealed partial class CEFootprintSystem
{
    [Dependency] private ITileDefinitionManager _tileDefs = default!;

    [Dependency] private EntityQuery<CETileFootprintComponent> _tilePrintQuery = default!;
    [Dependency] private EntityQuery<TimedDespawnComponent> _despawnQuery = default!;

    // Reusable buffer for tile prints of one tile, cleared before every use.
    private readonly List<Entity<TimedDespawnComponent>> _tilePrints = new();

    /// <summary>
    /// Leaves a temporary print on soft tiles (snow, sand) with <see cref="ContentTileDefinition.CEFootprintColor"/>.
    /// </summary>
    private void LeaveTilePrint(CEFootprintOwnerComponent owner,
        Carrier carrier,
        Entity<MapGridComponent> grid,
        Vector2i tile,
        Vector2 position,
        Angle rotation)
    {
        var tileRef = _map.GetTileRef(grid, tile);
        if (_tileDefs[tileRef.Tile.TypeId] is not ContentTileDefinition { CEFootprintColor: { } color })
            return;

        // Collect the old ones before spawning, so the new print is never the one removed.
        CollectTilePrints(grid, tile);

        if (!TrySpawnPrint(owner.TileFootprintProto, carrier.Sprite, color, grid, tile, position, rotation, out var print, out _)
            || !_tilePrintQuery.TryComp(print, out var tilePrint))
            return;

        // Remove the oldest prints over the limit: the ones with the least lifetime left.
        var excess = _tilePrints.Count + 1 - tilePrint.MaxPerTile;
        if (excess <= 0)
            return;

        _tilePrints.Sort((a, b) => a.Comp.Lifetime.CompareTo(b.Comp.Lifetime));
        for (var i = 0; i < excess && i < _tilePrints.Count; i++)
        {
            Del(_tilePrints[i]);
        }
    }

    /// <summary>
    /// Fills <see cref="_tilePrints"/> with the temporary tile prints anchored on the tile.
    /// </summary>
    private void CollectTilePrints(Entity<MapGridComponent> grid, Vector2i tile)
    {
        _tilePrints.Clear();

        var anchored = _map.GetAnchoredEntitiesEnumerator(grid, grid, tile);
        while (anchored.MoveNext(out var uid))
        {
            if (_tilePrintQuery.HasComp(uid) && _despawnQuery.TryComp(uid, out var despawn))
                _tilePrints.Add((uid.Value, despawn));
        }
    }
}
