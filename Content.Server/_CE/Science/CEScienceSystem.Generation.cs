using System.Linq;
using Content.Shared._CE.Hex;
using Content.Shared._CE.Science;
using Content.Shared._CE.Science.Prototypes;

namespace Content.Server._CE.Science;

public sealed partial class CEScienceSystem
{
    /// <summary>
    /// Procedurally generates a discovery's research puzzle: dead zones from the discovery's noise
    /// layers, one fixed target tile per aspect in <see cref="CEScienceDiscoveryPrototype.TargetAspects"/>
    /// placed along the outer ring, then a geometric connectivity guarantee (dead zones carved
    /// through) so no wall of dead zones can fully separate any two targets.
    /// </summary>
    public Dictionary<Vector2i, CEResearchMapTile> GenerateMap(CEScienceDiscoveryPrototype discovery)
    {
        var generation = discovery.Generation;
        var radius = generation.Radius;
        var tiles = new Dictionary<Vector2i, CEResearchMapTile>();

        // Randomized per generation (rather than mutating the shared prototype's noise instances)
        // so repeat projects for the same discovery don't always look identical.
        var noiseOffset = new Vector2i(_random.Next(-10000, 10000), _random.Next(-10000, 10000));

        foreach (var hex in CEHexMath.Spiral(Vector2i.Zero, radius))
        {
            if (IsDeadZone(generation, hex, noiseOffset))
                tiles[hex] = new CEResearchMapTile { DeadZone = true };
        }

        var targets = PlaceTargets(discovery, tiles);

        EnsureConnected(tiles, targets, radius);

        return tiles;
    }

    private static bool IsDeadZone(CEScienceMapGenerationParams generation, Vector2i hex, Vector2i noiseOffset)
    {
        foreach (var layer in generation.DeadZoneLayers)
        {
            var sample = layer.Noise.GetNoise(hex.X + noiseOffset.X, hex.Y + noiseOffset.Y);
            if (sample > layer.Threshold)
                return true;
        }

        return false;
    }

    private List<Vector2i> PlaceTargets(CEScienceDiscoveryPrototype discovery, Dictionary<Vector2i, CEResearchMapTile> tiles)
    {
        var radius = discovery.Generation.Radius;

        var aspects = discovery.TargetAspects.ToList();
        _random.Shuffle(aspects);

        var placed = new List<Vector2i>();
        foreach (var aspect in aspects)
        {
            var ring = CEHexMath.Ring(Vector2i.Zero, radius).ToList();
            _random.Shuffle(ring);

            var found = false;
            for (var minDistance = discovery.Generation.MinTargetDistance; minDistance >= 0 && !found; minDistance--)
            {
                foreach (var candidate in ring)
                {
                    if (!placed.All(other => CEHexMath.CubeDistance(candidate, other) >= minDistance))
                        continue;

                    tiles[candidate] = new CEResearchMapTile { Aspect = aspect, Fixed = true };
                    placed.Add(candidate);
                    found = true;
                    break;
                }
            }

            if (!found)
                Log.Warning($"CEScienceSystem: couldn't place target aspect {aspect} for discovery {discovery.ID} - no open ring tile.");
        }

        return placed;
    }

    // Guarantees every target is geometrically reachable from every other target through non-dead
    // tiles. Any target the flood fill doesn't reach gets connected via the cheapest path found -
    // one that touches as few dead-zone tiles as possible - rather than a blind straight line.
    private static void EnsureConnected(Dictionary<Vector2i, CEResearchMapTile> tiles, List<Vector2i> targets, int radius)
    {
        if (targets.Count == 0)
            return;

        var reached = FloodFill(tiles, targets[0], radius);

        foreach (var target in targets)
        {
            if (reached.Contains(target))
                continue;

            foreach (var hex in FindCarvePath(tiles, reached, target, radius))
            {
                if (tiles.TryGetValue(hex, out var tile) && tile.DeadZone)
                    tiles.Remove(hex);
            }

            reached = FloodFill(tiles, targets[0], radius);
        }
    }

    // 0-1 BFS from every already-reached tile at once: stepping into an open tile costs 0,
    // stepping into (and carving) a dead zone costs 1. Finds the path to the target that carves
    // the fewest dead zones, instead of a straight line that might cut through many for nothing.
    private static List<Vector2i> FindCarvePath(
        Dictionary<Vector2i, CEResearchMapTile> tiles,
        HashSet<Vector2i> reached,
        Vector2i target,
        int radius)
    {
        var cost = new Dictionary<Vector2i, int>();
        var previous = new Dictionary<Vector2i, Vector2i>();
        var deque = new LinkedList<Vector2i>();

        foreach (var start in reached)
        {
            cost[start] = 0;
            deque.AddLast(start);
        }

        while (deque.First is { } front)
        {
            var hex = front.Value;
            deque.RemoveFirst();

            if (hex == target)
                break;

            foreach (var neighbor in CEHexMath.Neighbors(hex))
            {
                if (CEHexMath.CubeDistance(neighbor, Vector2i.Zero) > radius)
                    continue;

                var stepCost = tiles.TryGetValue(neighbor, out var tile) && tile.DeadZone ? 1 : 0;
                var newCost = cost[hex] + stepCost;

                if (cost.TryGetValue(neighbor, out var existing) && existing <= newCost)
                    continue;

                cost[neighbor] = newCost;
                previous[neighbor] = hex;

                if (stepCost == 0)
                    deque.AddFirst(neighbor);
                else
                    deque.AddLast(neighbor);
            }
        }

        var path = new List<Vector2i>();
        var current = target;
        while (previous.TryGetValue(current, out var prior))
        {
            path.Add(current);
            current = prior;
        }

        return path;
    }

    private static HashSet<Vector2i> FloodFill(Dictionary<Vector2i, CEResearchMapTile> tiles, Vector2i start, int radius)
    {
        var visited = new HashSet<Vector2i> { start };
        var queue = new Queue<Vector2i>();
        queue.Enqueue(start);

        while (queue.TryDequeue(out var hex))
        {
            foreach (var neighbor in CEHexMath.Neighbors(hex))
            {
                if (visited.Contains(neighbor) || CEHexMath.CubeDistance(neighbor, Vector2i.Zero) > radius)
                    continue;

                if (tiles.TryGetValue(neighbor, out var tile) && tile.DeadZone)
                    continue;

                visited.Add(neighbor);
                queue.Enqueue(neighbor);
            }
        }

        return visited;
    }
}
