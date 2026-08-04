using System.Linq;
using Content.Shared._CE.Science;
using Content.Shared._CE.Science.Prototypes;
using Content.Shared.Destructible.Thresholds;
using Robust.Shared.Random;

namespace Content.Server._CE.Science;

public sealed partial class CEScienceSystem
{
    [Dependency] private IRobustRandom _random = default!;

    /// <summary>
    /// The starting 3x3 square (matching <see cref="CESharedScienceSystem"/>'s initial reveal
    /// radius of 1) that's always forced clear of dead zones, so the player never starts boxed in.
    /// </summary>
    private const int StartClearRadius = 1;

    /// <summary>
    /// Procedurally generates a science area's research map: dead zones from the area's noise
    /// layers, then one star cell per <see cref="CEScienceDiscoveryPrototype"/> belonging to this
    /// area, placed within its <see cref="CEScienceDiscoveryDifficultyPrototype.SpawnDistance"/>
    /// band. A star doesn't remember which discovery reserved its slot - only its rarity; which
    /// discovery ends up there is decided later, when a player opens it.
    /// Finally, guarantees every star is actually reachable from the start by carving a path
    /// through dead zones to any that aren't.
    /// </summary>
    private Dictionary<Vector2i, CEScienceMapCell> GenerateArea(CEScienceAreaPrototype area)
    {
        var discoveries = _proto.EnumeratePrototypes<CEScienceDiscoveryPrototype>()
            .Where(discovery => discovery.Area == area.ID)
            .ToList();

        var radius = 0;
        foreach (var discovery in discoveries)
        {
            if (_proto.TryIndex(discovery.Rarity, out var rarity))
                radius = Math.Max(radius, rarity.SpawnDistance.Max);
        }

        radius += area.GenerationMargin;
        var cells = new Dictionary<Vector2i, CEScienceMapCell>();

        for (var x = -radius; x <= radius; x++)
        for (var y = -radius; y <= radius; y++)
        {
            var coordinate = new Vector2i(x, y);
            if (IsDeadZone(area, coordinate))
                cells[coordinate] = new CEScienceDeadZoneCell();
        }

        // The player always starts able to see this square (see CESharedScienceSystem's initial
        // RevealArea call) - never let noise strand them in dead zones from the very first cell.
        for (var x = -StartClearRadius; x <= StartClearRadius; x++)
        for (var y = -StartClearRadius; y <= StartClearRadius; y++)
            cells.Remove(new Vector2i(x, y));

        var placedStars = new List<Vector2i>();
        foreach (var discovery in discoveries)
        {
            if (!_proto.TryIndex(discovery.Rarity, out var rarity))
                continue;

            if (!TryPlaceStar(cells, rarity.SpawnDistance, out var coordinate))
            {
                Log.Warning($"CEScienceSystem: couldn't place a star for discovery {discovery.ID} for area {area.ID} - no empty cell within its rarity's spawn band or the next ring out.");
                continue;
            }

            cells[coordinate] = new CEScienceStarCell(discovery.Rarity);
            placedStars.Add(coordinate);
        }

        EnsureReachable(cells, radius, placedStars);

        return cells;
    }

    /// <summary>
    /// Flood-fills from the origin over every non-dead-zone cell within <paramref name="radius"/>.
    /// Any star not caught by that flood fill gets a path carved to it (dead zones cleared along a
    /// straight 8-directional line) from whichever reachable cell is closest.
    /// </summary>
    private static void EnsureReachable(Dictionary<Vector2i, CEScienceMapCell> cells, int radius, List<Vector2i> stars)
    {
        var reachable = FloodFillReachable(cells, radius);

        foreach (var star in stars)
        {
            if (reachable.Contains(star))
                continue;

            var nearest = FindNearestReachable(reachable, star);
            CarvePath(cells, reachable, nearest, star);
        }
    }

    private static HashSet<Vector2i> FloodFillReachable(Dictionary<Vector2i, CEScienceMapCell> cells, int radius)
    {
        var visited = new HashSet<Vector2i> { Vector2i.Zero };
        var queue = new Queue<Vector2i>();
        queue.Enqueue(Vector2i.Zero);

        while (queue.TryDequeue(out var current))
        {
            for (var dx = -1; dx <= 1; dx++)
            for (var dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0)
                    continue;

                var neighbor = current + new Vector2i(dx, dy);

                if (Math.Abs(neighbor.X) > radius || Math.Abs(neighbor.Y) > radius)
                    continue;

                if (visited.Contains(neighbor))
                    continue;

                if (cells.TryGetValue(neighbor, out var cell) && cell is CEScienceDeadZoneCell)
                    continue;

                visited.Add(neighbor);
                queue.Enqueue(neighbor);
            }
        }

        return visited;
    }

    private static Vector2i FindNearestReachable(HashSet<Vector2i> reachable, Vector2i target)
    {
        var best = Vector2i.Zero;
        var bestDistance = int.MaxValue;

        foreach (var candidate in reachable)
        {
            var delta = candidate - target;
            var distance = Math.Max(Math.Abs(delta.X), Math.Abs(delta.Y));

            if (distance >= bestDistance)
                continue;

            bestDistance = distance;
            best = candidate;
        }

        return best;
    }

    /// <summary>
    /// Walks a straight 8-directional line from <paramref name="from"/> (already reachable) to
    /// <paramref name="to"/>, clearing any dead zone along the way and marking every stepped-on
    /// cell as reachable.
    /// </summary>
    private static void CarvePath(Dictionary<Vector2i, CEScienceMapCell> cells, HashSet<Vector2i> reachable, Vector2i from, Vector2i to)
    {
        var current = from;

        while (current != to)
        {
            var delta = to - current;
            var step = new Vector2i(Math.Sign(delta.X), Math.Sign(delta.Y));
            current += step;

            if (cells.TryGetValue(current, out var cell) && cell is CEScienceDeadZoneCell)
                cells.Remove(current);

            reachable.Add(current);
        }
    }

    /// <summary>
    /// A cell is a dead zone if any of the area's noise layers reads above that layer's threshold
    /// at that coordinate.
    /// </summary>
    private static bool IsDeadZone(CEScienceAreaPrototype area, Vector2i coordinate)
    {
        foreach (var layer in area.DeadZoneLayers)
        {
            if (layer.Noise.GetNoise(coordinate.X, coordinate.Y) > layer.Threshold)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Tries to find an empty coordinate within the square annulus <paramref name="band"/> cells
    /// (Chebyshev distance) from the origin. Falls back to the next ring out once if the whole band
    /// is completely full.
    /// </summary>
    private bool TryPlaceStar(Dictionary<Vector2i, CEScienceMapCell> cells, MinMax band, out Vector2i coordinate)
    {
        var candidates = new List<Vector2i>();
        for (var ringRadius = band.Min; ringRadius <= band.Max; ringRadius++)
            candidates.AddRange(GetRing(ringRadius).Where(candidate => !cells.ContainsKey(candidate)));

        if (candidates.Count == 0)
            candidates.AddRange(GetRing(band.Max + 1).Where(candidate => !cells.ContainsKey(candidate)));

        if (candidates.Count == 0)
        {
            coordinate = default;
            return false;
        }

        coordinate = _random.Pick(candidates);
        return true;
    }

    /// <summary>
    /// Every coordinate on the perimeter of the square with Chebyshev radius
    /// <paramref name="radius"/> around the origin.
    /// </summary>
    private static IEnumerable<Vector2i> GetRing(int radius)
    {
        if (radius <= 0)
        {
            yield return Vector2i.Zero;
            yield break;
        }

        for (var x = -radius; x <= radius; x++)
        {
            yield return new Vector2i(x, -radius);
            yield return new Vector2i(x, radius);
        }

        for (var y = -radius + 1; y <= radius - 1; y++)
        {
            yield return new Vector2i(-radius, y);
            yield return new Vector2i(radius, y);
        }
    }
}
