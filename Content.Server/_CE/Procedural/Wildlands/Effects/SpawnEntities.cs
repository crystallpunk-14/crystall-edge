using Content.Server._CE.Procedural.Generation;
using Content.Server._CE.Procedural.Generation.Masks;
using Content.Shared.Destructible.Thresholds;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._CE.Procedural.Wildlands.Effects;

/// <summary>
/// Spawns <see cref="Count"/> of <see cref="Entity"/> on random tiles of the region that pass <see cref="Mask"/>,
/// picked across all levels at once. Never deletes anything; running out of tiles just spawns fewer.
/// </summary>
public sealed partial class SpawnEntities : ICEWildlandsModifierEffect
{
    [DataField(required: true)]
    public EntProtoId Entity;

    [DataField]
    public MinMax Count = new(1, 1);

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

        var count = Math.Min(Count.Next(random), candidates.Count);
        for (var i = 0; i < count; i++)
        {
            var (map, tile) = candidates[i];
            var grid = entMan.GetComponent<MapGridComponent>(map);
            entMan.SpawnEntity(Entity, context.Map.GridTileToLocal(map, grid, tile));
        }
    }
}
