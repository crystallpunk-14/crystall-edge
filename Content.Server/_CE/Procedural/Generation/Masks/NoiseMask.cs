using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Noise;

namespace Content.Server._CE.Procedural.Generation.Masks;

/// <summary>
/// Matches wherever seeded noise clears <see cref="Threshold"/> — the "veins in random places"
/// mask. <see cref="Seed"/> is added to the run's seed, same convention as
/// <see cref="Layers.TileNoiseDistanceLayer"/>: leave at 0 to share the run's seed with other
/// masks/layers, set a different value to decorrelate veins that would otherwise line up.
/// </summary>
public sealed partial class NoiseMask : ICETileMask
{
    [DataField(required: true)]
    public FastNoiseLite Noise = new();

    [DataField(required: true)]
    public float Threshold;

    [DataField]
    public int Seed;

    public override bool Matches(CEProceduralGenerationContext context, EntityUid map, MapGridComponent grid, Vector2i tile, Tile currentTile)
    {
        Noise.SetSeed(context.Seed + Seed);
        return Noise.GetNoise(tile.X, tile.Y) >= Threshold;
    }
}
