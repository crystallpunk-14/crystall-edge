using System.Threading.Tasks;
using Content.Server._CE.Procedural.Generation.Masks;
using Content.Shared.Parallax.Biomes;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;

namespace Content.Server._CE.Procedural.Generation.Layers;

/// <summary>
/// Overlays a biome (tile variants, decals, entities) onto every tile of the map that passes
/// <see cref="Mask"/>. The preceding tile layer's output typically doubles as the region mask via a
/// <see cref="TileMask"/> — a deliberate, simple approach: which tiles are present defines where
/// the biome lands.
/// </summary>
public sealed partial class FillBiomeLayer : ICEProceduralLayer
{
    [DataField(required: true)]
    public ProtoId<BiomeTemplatePrototype> Biome;

    /// <summary>
    /// Only tiles where every mask matches (each respecting its own <see cref="ICETileMask.Inverted"/>)
    /// receive the biome.
    /// </summary>
    [DataField(required: true)]
    public List<ICETileMask> Mask = new();

    /// <summary>
    /// Added to the generation seed for this layer's biome sampling. Leave at 0 to share the run's
    /// seed with other layers; set a different value per layer to decorrelate biomes that would
    /// otherwise generate the same pattern from the same seed.
    /// </summary>
    [DataField]
    public int Seed;

    public async Task Apply(CEProceduralGenerationContext context, EntityUid map)
    {
        var grid = context.EntityManager.GetComponent<MapGridComponent>(map);
        var biome = context.Prototype.Index(Biome);

        // Snapshot the masked tiles up front — we mutate tiles below, which would disturb a live
        // enumerator.
        var targets = new List<Vector2i>();
        var enumerator = context.Map.GetAllTilesEnumerator(map, grid);
        while (enumerator.MoveNext(out var tileRef))
        {
            var indices = tileRef.Value.GridIndices;

            var passes = true;
            foreach (var tileMask in Mask)
            {
                if (tileMask.Matches(context, map, grid, indices, tileRef.Value.Tile) == tileMask.Inverted)
                {
                    passes = false;
                    break;
                }
            }

            if (passes)
                targets.Add(indices);
        }

        var seed = context.Seed + Seed;

        var processed = 0;
        foreach (var indices in targets)
        {
            if (!context.Biome.TryGetTile(indices, biome.Layers, seed, (map, grid), out var tile))
                continue;

            context.Map.SetTile(map, grid, indices, tile.Value);

            if (context.Biome.TryGetDecals(indices, biome.Layers, seed, (map, grid), out var decals))
            {
                foreach (var decal in decals)
                {
                    context.Decals.TryAddDecal(decal.ID, new EntityCoordinates(map, decal.Position), out _);
                }
            }

            if (context.Biome.TryGetEntity(indices, biome.Layers, tile.Value, seed, (map, grid), out var entityProto))
            {
                var center = indices + grid.TileSizeHalfVector;
                context.EntityManager.SpawnEntity(entityProto, new EntityCoordinates(map, center));
            }

            if (++processed % 64 == 0)
            {
                await context.Suspend();
                context.Cancellation.ThrowIfCancellationRequested();
            }
        }
    }
}
