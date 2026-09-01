using System.Threading.Tasks;
using Content.Shared.Parallax.Biomes;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;

namespace Content.Server._CE.Demiplane.Generation;

/// <summary>
/// Debug generator: one flat biome-filled map per entry in <see cref="Layers"/>, stacked below the
/// demiplane entry point in list order (index 0 = nearest the entry point). No dungeon layering, no
/// vertical connectivity between levels — just enough to have a walkable multi-level stage for testing.
/// </summary>
public sealed partial class CEDebugFlatWorldGenerator : ICEDemiplaneLocationGenerator
{
    /// <summary>
    /// One entry per generated z-level. E.g. [Forest, Caves, Caves] generates a forest level
    /// followed by two cave levels below it.
    /// </summary>
    [DataField(required: true)]
    public List<ProtoId<BiomeTemplatePrototype>> Layers = new();

    /// <summary>
    /// Every level is a <see cref="Size"/> × <see cref="Size"/> square filled from (0, 0).
    /// </summary>
    [DataField]
    public int Size = 50;

    public async Task<List<EntityUid>> Generate(CEDemiplaneGenerationContext context)
    {
        var maps = new List<EntityUid>();

        foreach (var biomeId in Layers)
        {
            var biome = context.Prototype.Index(biomeId);
            var mapUid = context.Map.CreateMap(out _, runMapInit: false);
            var grid = context.EntityManager.EnsureComponent<MapGridComponent>(mapUid);

            var row = new List<(Vector2i, Tile)>(Size);

            for (var y = 0; y < Size; y++)
            {
                row.Clear();

                for (var x = 0; x < Size; x++)
                {
                    var indices = new Vector2i(x, y);
                    if (context.Biome.TryGetTile(indices, biome.Layers, context.Seed, (mapUid, grid), out var tile))
                        row.Add((indices, tile.Value));
                }

                if (row.Count > 0)
                    context.Map.SetTiles(mapUid, grid, row);

                await context.Suspend();
                context.Cancellation.ThrowIfCancellationRequested();
            }

            maps.Add(mapUid);
        }

        return maps;
    }
}
