using Content.Shared.Destructible.Thresholds;
using Robust.Shared.Random;

namespace Content.Server._CE.Procedural.Generation.Points;

/// <summary>
/// Named set of positions rolled once per generation run and shared by every layer and level through
/// <see cref="CEProceduralGenerationContext.Points"/> — e.g. where a sub-biome sits, identical on every z-level.
/// </summary>
[DataDefinition]
public sealed partial class CEProceduralPoint
{
    [DataField(required: true)]
    public string Id = string.Empty;

    [DataField(required: true)]
    public ICEPointArea Area = default!;

    /// <summary>
    /// Chance the point gets any positions at all.
    /// </summary>
    [DataField]
    public float Prob = 1f;

    [DataField]
    public MinMax Count = new(1, 1);

    /// <summary>
    /// Minimum distance to every position already picked, by this point or any declared before it.
    /// </summary>
    [DataField]
    public float MinDistance;

    private const int MaxAttempts = 10;

    /// <summary>
    /// Resolves <paramref name="points"/> in declaration order into absolute tile positions around
    /// <paramref name="center"/>. A position that finds no spot clear of <see cref="MinDistance"/> is dropped.
    /// </summary>
    public static Dictionary<string, List<Vector2i>> Resolve(List<CEProceduralPoint> points, Vector2i center, int seed)
    {
        IRobustRandom random = new RobustRandom();
        random.SetSeed(seed);

        var resolved = new Dictionary<string, List<Vector2i>>();
        var taken = new List<Vector2i>();
        foreach (var point in points)
        {
            var positions = new List<Vector2i>();
            resolved[point.Id] = positions;

            if (!random.Prob(point.Prob))
                continue;

            var count = point.Count.Next(random);
            for (var i = 0; i < count; i++)
            {
                for (var attempt = 0; attempt < MaxAttempts; attempt++)
                {
                    var candidate = center + point.Area.Sample(random);
                    if (!point.IsClear(candidate, taken))
                        continue;

                    positions.Add(candidate);
                    taken.Add(candidate);
                    break;
                }
            }
        }

        return resolved;
    }

    private bool IsClear(Vector2i candidate, List<Vector2i> taken)
    {
        var minSquared = MinDistance * MinDistance;
        foreach (var other in taken)
        {
            if ((candidate - other).LengthSquared < minSquared)
                return false;
        }

        return true;
    }
}
