using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Noise;

namespace Content.Server._CE.Procedural.Generation.Masks;

/// <summary>
/// Matches tiles within <see cref="Radius"/> of the context center, or of the nearest position of
/// <see cref="Point"/> when set — so effects that roll across the whole map can still be confined to a point's area.
/// <see cref="Noise"/> pushes the edge in and out by up to <see cref="NoiseAmplitude"/> tiles; its seed follows the
/// same convention as <see cref="NoiseMask"/>.
/// </summary>
public sealed partial class RadiusMask : ICETileMask
{
    private static readonly ISawmill Sawmill = Logger.GetSawmill("ce.procedural");

    [DataField(required: true)]
    public float Radius;

    [DataField]
    public string? Point;

    [DataField]
    public FastNoiseLite? Noise;

    [DataField]
    public float NoiseAmplitude;

    [DataField]
    public int Seed;

    private bool _reportedUnknownPoint;

    public override bool Matches(CEProceduralGenerationContext context, EntityUid map, MapGridComponent grid, Vector2i tile, Tile currentTile)
    {
        var radius = Radius;
        if (Noise is not null)
        {
            Noise.SetSeed(context.Seed + Seed);
            var local = tile - context.Center;
            radius += Noise.GetNoise(local.X, local.Y) * NoiseAmplitude;
        }

        var radiusSquared = radius * radius;

        if (Point is null)
            return (tile - context.Center).LengthSquared <= radiusSquared;

        if (!context.Points.TryGetValue(Point, out var positions))
        {
            // Called per tile; one error per mask is enough.
            if (!_reportedUnknownPoint)
                Sawmill.Error($"{nameof(RadiusMask)} references unknown point '{Point}'.");

            _reportedUnknownPoint = true;
            return false;
        }

        foreach (var position in positions)
        {
            if ((tile - position).LengthSquared <= radiusSquared)
                return true;
        }

        return false;
    }
}
