using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Server._CE.ZCollapse;

public sealed partial class CEZCollapseSystem
{
    /// <summary>
    /// Increase flood-fill: raises a tile's stored stability when a source reaches it with a higher
    /// value, and always attempts to push out to its neighbors using its real current value — even
    /// when that value didn't need to change. This second part matters for Depropagate's relight
    /// pass: a protected seed tile that survived a de-propagation wave untouched (same value as
    /// before) must still re-push its flood into neighbors that *were* just cleared, or the relight
    /// self-correction Depropagate relies on can't happen (a tie/over-eager clear would otherwise
    /// leave the reflood stuck at the seed tile itself).
    /// </summary>
    private HashSet<Vector2i> Propagate(MapGridComponent grid, CEGridStabilityComponent comp, Queue<(Vector2i Tile, int Value)> queue)
    {
        var touched = new HashSet<Vector2i>();

        while (queue.TryDequeue(out var entry))
        {
            var (tile, value) = entry;
            if (value <= 0)
                continue;

            var existing = comp.Stability.GetValueOrDefault(tile, 0);
            if (value > existing)
            {
                comp.Stability[tile] = value;
                touched.Add(tile);
            }
            else
            {
                value = existing; // use the tile's real value to re-push into cleared neighbors
            }

            foreach (var neighbor in GetFourNeighbors(tile))
            {
                if (!IsTileAlive(grid, neighbor))
                    continue;

                var next = value - 1;
                if (next <= 0)
                    continue; // would be dead on arrival — don't create a 0-entry

                if (next > comp.Stability.GetValueOrDefault(neighbor, 0))
                    queue.Enqueue((neighbor, next));
            }
        }

        return touched;
    }
}
