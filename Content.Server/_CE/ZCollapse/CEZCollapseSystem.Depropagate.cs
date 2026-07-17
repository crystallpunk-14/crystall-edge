using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Server._CE.ZCollapse;

public sealed partial class CEZCollapseSystem
{
    /// <summary>
    /// De-propagation ("unlight" BFS, the classic Minecraft block-light-removal algorithm): clears a
    /// popped tile and any neighbor whose value could only have derived from it
    /// (<c>neighborValue == poppedValue - 1</c>), while neighbors with an independent/stronger value
    /// (a live seed, or simply too high to have come from the popped tile) are collected as a
    /// re-light boundary and re-flooded via <see cref="Propagate"/> at the end, self-correcting any
    /// over-eager clearing near ties between two legitimate sources.
    /// </summary>
    private HashSet<Vector2i> Depropagate(MapGridComponent grid, CEGridStabilityComponent comp, Queue<(Vector2i Tile, int Value)> popQueue)
    {
        var touched = new HashSet<Vector2i>();
        var processed = new HashSet<Vector2i>();
        var relight = new Queue<(Vector2i Tile, int Value)>();

        while (popQueue.TryDequeue(out var entry))
        {
            var (tile, poppedValue) = entry;
            if (!processed.Add(tile))
                continue;

            comp.Stability.Remove(tile);
            touched.Add(tile);

            // Tile is still an independent seed (possibly at a lower value than before) — refill from it.
            if (comp.Seeds.TryGetValue(tile, out var ownSeed))
                relight.Enqueue((tile, ownSeed));

            foreach (var neighbor in GetFourNeighbors(tile))
            {
                if (!comp.Stability.TryGetValue(neighbor, out var neighborValue))
                    continue; // no data there, nothing to do

                if (comp.Seeds.ContainsKey(neighbor))
                {
                    // Independently sourced — never cleared by a neighbor pop, but re-flood from it
                    // in case its own reach into other neighbors needs to reassert dominance.
                    relight.Enqueue((neighbor, neighborValue));
                }
                else if (neighborValue == poppedValue - 1)
                {
                    // Could only have derived from `tile` — clear it too.
                    popQueue.Enqueue((neighbor, neighborValue));
                }
                else if (neighborValue > poppedValue - 1)
                {
                    // Higher than `tile` could have supplied — independent, stronger source nearby.
                    relight.Enqueue((neighbor, neighborValue));
                }
                // neighborValue < poppedValue - 1: weaker independent path, leave untouched.
            }
        }

        if (relight.Count > 0)
            touched.UnionWith(Propagate(grid, comp, relight));

        return touched;
    }
}
