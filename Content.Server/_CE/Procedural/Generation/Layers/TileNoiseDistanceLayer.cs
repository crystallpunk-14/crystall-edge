using System.Threading.Tasks;
using Content.Server._CE.Procedural.Generation.Distance;
using Content.Shared.Maps;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Noise;
using Robust.Shared.Prototypes;

namespace Content.Server._CE.Procedural.Generation.Layers;

/// <summary>
/// Paints tiles by noise-and-distance-from-center, the "island from noise" technique: the seeded
/// noise is blended toward <c>1 - distance</c> (see <see cref="DistanceConfig"/>), then each
/// <see cref="Fill"/> entry places its tile where the blended value clears its threshold. Reusing the
/// same noise across several layers (via a YAML anchor) with a rising threshold at lower stack levels
/// is what tapers the shape into a point going down.
/// </summary>
public sealed partial class TileNoiseDistanceLayer : ICEProceduralLayer
{
    /// <summary>
    /// Area to sample, centered on (0, 0).
    /// </summary>
    [DataField(required: true)]
    public Vector2i Size;

    /// <summary>
    /// Radial falloff blended into the noise. Null = pure noise with no center bias.
    /// </summary>
    [DataField]
    public ICEDistanceConfig? DistanceConfig;

    [DataField(required: true)]
    public List<TileNoiseFill> Fill = new();

    public async Task Apply(CEProceduralGenerationContext context, EntityUid map)
    {
        var grid = context.EntityManager.GetComponent<MapGridComponent>(map);

        foreach (var fill in Fill)
        {
            fill.Noise.SetSeed(context.Seed);
        }

        var area = Box2i.FromDimensions(-Size / 2, Size);
        var width = (float) area.Width;
        var height = (float) area.Height;

        var tiles = new List<(Vector2i, Tile)>();

        for (var x = area.Left; x <= area.Right; x++)
        {
            for (var y = area.Bottom; y <= area.Top; y++)
            {
                foreach (var fill in Fill)
                {
                    var value = fill.Noise.GetNoise(x, y);

                    if (DistanceConfig != null)
                    {
                        // Position in the range -1 -> 1 relative to the center.
                        var dx = 2f * x / width;
                        var dy = 2f * y / height;
                        var distance = DistanceConfig.GetDistance(dx, dy);
                        value = MathHelper.Lerp(value, 1f - distance, DistanceConfig.BlendWeight);
                    }

                    if (value < fill.Threshold)
                        continue;

                    var tileDef = context.TileDefManager[fill.Tile.Id];
                    tiles.Add((new Vector2i(x, y), new Tile(tileDef.TileId)));
                    // First matching fill wins — earlier entries take precedence over later ones.
                    break;
                }
            }

            await context.Suspend();
            context.Cancellation.ThrowIfCancellationRequested();
        }

        context.Map.SetTiles(map, grid, tiles);
    }
}

[DataRecord]
public partial record struct TileNoiseFill
{
    /// <summary>
    /// Tile is placed where the blended noise value is at or above this.
    /// </summary>
    [DataField]
    public float Threshold;

    [DataField(required: true)]
    public ProtoId<ContentTileDefinition> Tile;

    [DataField(required: true)]
    public FastNoiseLite Noise;
}
