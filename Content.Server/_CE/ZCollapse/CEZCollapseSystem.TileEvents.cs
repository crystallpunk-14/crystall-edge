namespace Content.Server._CE.ZCollapse;

public sealed partial class CEZCollapseSystem
{
    private void InitializeTileEvents()
    {
        SubscribeLocalEvent<CEGridStabilityComponent, TileChangedEvent>(OnTileChanged);
    }

    /// <summary>
    /// Handles externally-caused tile add/remove (RCD, explosion, etc). Collapse-caused removals
    /// never need reprocessing here: by the time <see cref="ReapDeadTiles"/> calls SetTiles, the
    /// tile already has no Stability entry, so the "still has a value" check below skips it.
    /// </summary>
    private void OnTileChanged(Entity<CEGridStabilityComponent> ent, ref TileChangedEvent args)
    {
        if (!_gridQuery.TryGetComponent(ent, out var grid))
            return;

        foreach (var change in args.Changes)
        {
            var tile = change.GridIndices;

            if (change.NewTile.IsEmpty && !change.OldTile.IsEmpty)
            {
                if (!ent.Comp.Stability.TryGetValue(tile, out var existing))
                    continue; // no stability data — collapse-caused, already fully processed

                var popQueue = new Queue<(Vector2i, int)>();
                popQueue.Enqueue((tile, existing));
                RunDepropagate(ent, grid, ent.Comp, popQueue);
            }
            else if (!change.NewTile.IsEmpty && change.OldTile.IsEmpty)
            {
                // Newly placed tile — inherit from whatever alive neighbors already reach it.
                var best = 0;
                foreach (var neighbor in GetFourNeighbors(tile))
                {
                    if (ent.Comp.Stability.TryGetValue(neighbor, out var neighborValue))
                        best = Math.Max(best, neighborValue - 1);
                }

                if (best <= 0)
                {
                    // No neighbor can support it — reap immediately. Without this, a tile placed in
                    // a dead zone never enters any BFS pass (Propagate never even starts), so nothing
                    // would ever reap it and it would just sit there forever, unrelated to the
                    // stability system — effectively lets players build infinitely outward.
                    ReapDeadTiles(ent, grid, ent.Comp, new[] { tile });
                    continue;
                }

                var queue = new Queue<(Vector2i, int)>();
                queue.Enqueue((tile, best));
                RunPropagate(ent, grid, ent.Comp, queue);
            }
        }
    }
}
