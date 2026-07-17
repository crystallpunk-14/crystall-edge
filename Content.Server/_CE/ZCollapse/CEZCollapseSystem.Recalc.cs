using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Server._CE.ZCollapse;

// Consistency-repair fallback: the incremental Propagate/Depropagate passes are meant to always stay
// correct, so this should only ever be needed to recover from an implementation bug, not as a
// routine part of normal play. Exposed via "cezcollapserecalc" for admins.
public sealed partial class CEZCollapseSystem
{
    public void ForceRecalculateGrid(EntityUid gridUid)
    {
        if (!_stabilityQuery.TryGetComponent(gridUid, out var comp) || !_gridQuery.TryGetComponent(gridUid, out var grid))
            return;

        comp.Stability.Clear();
        comp.Seeds.Clear();
        comp.CoreSeeds.Clear();
        comp.BridgeSeedsFromAbove.Clear();
        comp.BridgeSeedsFromBelow.Clear();

        var queue = new Queue<(Vector2i, int)>();
        var coreQuery = AllEntityQuery<CEGridStabilityCoreComponent, TransformComponent>();
        while (coreQuery.MoveNext(out _, out var core, out var xform))
        {
            if (xform.GridUid != gridUid || !xform.Anchored)
                continue;

            var tile = _map.TileIndicesFor(gridUid, grid, xform.Coordinates);
            comp.CoreSeeds[tile] = Math.Max(comp.CoreSeeds.GetValueOrDefault(tile, 0), core.LevitationForce);
        }

        foreach (var (tile, value) in comp.CoreSeeds)
        {
            comp.Seeds[tile] = value;
            queue.Enqueue((tile, value));
        }

        var touched = Propagate(grid, comp, queue);
        ReapDeadTiles(gridUid, grid, comp, touched);
        _debugDirtyGrids.Add(gridUid);

        // Propagate structurally can only reach tiles connected to a seed — a platform with no path
        // to any Core/bridge at all never enters that BFS, so it's missing from `touched` too.
        ReapOrphanTiles(gridUid);

        // Re-derive every Support bridge touching this grid — its own supports (bridging up), and
        // any on the grid below (bridging up into this one) — via the normal bridge logic. Whatever
        // that touches on other grids cascades further through the usual per-tick bridge-recheck
        // queue, it doesn't need to be forced here too.
        var tilesToRecheck = new HashSet<(EntityUid Grid, Vector2i Tile)>();
        CollectSupportTiles(gridUid, grid, tilesToRecheck);

        if (_zMapQuery.TryGetComponent(gridUid, out var zMap) &&
            _zLevel.TryMapDown((gridUid, zMap), out var belowMap) &&
            _gridQuery.TryGetComponent(belowMap.Owner, out var belowGrid))
        {
            CollectSupportTiles(belowMap.Owner, belowGrid, tilesToRecheck);
        }

        foreach (var (supportGrid, tile) in tilesToRecheck)
            RecomputeBridge(supportGrid, tile);
    }

    private void CollectSupportTiles(EntityUid gridUid, MapGridComponent grid, HashSet<(EntityUid Grid, Vector2i Tile)> result)
    {
        var query = AllEntityQuery<CEGridStabilitySupportComponent, TransformComponent>();
        while (query.MoveNext(out _, out _, out var xform))
        {
            if (xform.GridUid != gridUid || !xform.Anchored)
                continue;

            result.Add((gridUid, _map.TileIndicesFor(gridUid, grid, xform.Coordinates)));
        }
    }
}
