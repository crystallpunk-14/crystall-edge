namespace Content.Shared._CE.Hex;

/// <summary>
/// Pure axial hex-coordinate math, using <see cref="Vector2i"/> as (Q, R). Not tied to any
/// specific feature - anything needing a hex grid can reuse this.
/// </summary>
public static class CEHexMath
{
    public static readonly Vector2i[] Directions =
    {
        new(1, 0), new(1, -1), new(0, -1),
        new(-1, 0), new(-1, 1), new(0, 1),
    };

    public static Vector2i Neighbor(Vector2i hex, int direction) => hex + Directions[direction];

    public static IEnumerable<Vector2i> Neighbors(Vector2i hex)
    {
        foreach (var direction in Directions)
            yield return hex + direction;
    }

    public static int CubeDistance(Vector2i a, Vector2i b)
    {
        var dq = a.X - b.X;
        var dr = a.Y - b.Y;
        return (Math.Abs(dq) + Math.Abs(dq + dr) + Math.Abs(dr)) / 2;
    }

    /// <summary>Every hex at exactly <paramref name="radius"/> distance from <paramref name="center"/>.</summary>
    public static IEnumerable<Vector2i> Ring(Vector2i center, int radius)
    {
        if (radius <= 0)
        {
            yield return center;
            yield break;
        }

        var hex = center + Directions[4] * radius;
        for (var side = 0; side < 6; side++)
        {
            for (var step = 0; step < radius; step++)
            {
                yield return hex;
                hex = Neighbor(hex, side);
            }
        }
    }

    /// <summary>Every hex within <paramref name="radius"/> of <paramref name="center"/>, center first.</summary>
    public static IEnumerable<Vector2i> Spiral(Vector2i center, int radius)
    {
        yield return center;
        for (var r = 1; r <= radius; r++)
        {
            foreach (var hex in Ring(center, r))
                yield return hex;
        }
    }
}
