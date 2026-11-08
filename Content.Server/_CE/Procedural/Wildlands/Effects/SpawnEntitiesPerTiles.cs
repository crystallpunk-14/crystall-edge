using Content.Server._CE.Procedural.Generation;
using Content.Server._CE.Procedural.Generation.Masks;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._CE.Procedural.Wildlands.Effects;

/// <summary>
/// Spawns about one <see cref="Entity"/> per <see cref="TilesPerSpawn"/> tiles of the region that pass <see cref="Mask"/>,
/// picked across all levels at once, so the amount scales with the map. Never deletes anything.
/// </summary>
public sealed partial class SpawnEntitiesPerTiles : ICEWildlandsModifierEffect
{
    [DataField(required: true)]
    public EntProtoId Entity;

    [DataField(required: true)]
    public float TilesPerSpawn;

    [DataField]
    public float TilesPerSpawnStdDev;

    [DataField]
    public List<ICETileMask> Mask = new();

    public void Apply(CEProceduralGenerationContext context, IReadOnlyDictionary<int, EntityUid> levels)
    {
        var entMan = context.EntityManager;
        IRobustRandom random = new RobustRandom();
        random.SetSeed(context.Seed);

        var candidates = new List<(EntityUid Map, Vector2i Tile)>();
        foreach (var map in levels.Values)
        {
            var grid = entMan.GetComponent<MapGridComponent>(map);
            foreach (var tile in ICETileMask.CollectTiles(context, map, grid, Mask))
            {
                candidates.Add((map, tile));
            }
        }

        // Candidates come out in grid order; shuffling keeps the picks seed-dependent but order-independent.
        random.Shuffle(candidates);

        var tilesPerSpawn = Math.Max(random.NextGaussian(TilesPerSpawn, TilesPerSpawnStdDev), 1);
        var count = Math.Min((int) Math.Round(candidates.Count / tilesPerSpawn), candidates.Count);
        for (var i = 0; i < count; i++)
        {
            var (map, tile) = candidates[i];
            var grid = entMan.GetComponent<MapGridComponent>(map);
            entMan.SpawnEntity(Entity, context.Map.GridTileToLocal(map, grid, tile));
        }
    }
}
