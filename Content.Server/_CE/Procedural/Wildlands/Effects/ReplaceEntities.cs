using Content.Server._CE.Procedural.Generation;
using Content.Shared.Whitelist;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._CE.Procedural.Wildlands.Effects;

/// <summary>
/// Replaces whitelisted anchored entities across all levels of the region: exactly <see cref="Count"/>, or about one per <see cref="EntitiesPerReplacement"/>.
/// </summary>
public sealed partial class ReplaceEntities : ICEWildlandsModifierEffect
{
    [DataField(required: true)]
    public EntityWhitelist Whitelist = new();

    [DataField(required: true)]
    public EntProtoId Replacement;

    [DataField]
    public int? Count;

    [DataField]
    public float? EntitiesPerReplacement;

    [DataField]
    public float EntitiesPerReplacementStdDev;

    public void Apply(CEProceduralGenerationContext context, IReadOnlyDictionary<int, EntityUid> levels)
    {
        var entMan = context.EntityManager;
        var whitelist = entMan.System<EntityWhitelistSystem>();
        IRobustRandom random = new RobustRandom();
        random.SetSeed(context.Seed);

        var candidates = new List<EntityUid>();
        foreach (var map in levels.Values)
        {
            var grid = entMan.GetComponent<MapGridComponent>(map);
            var tiles = context.Map.GetAllTilesEnumerator(map, grid);
            while (tiles.MoveNext(out var tileRef))
            {
                var indices = tileRef.Value.GridIndices;
                if (!context.Region.Contains(indices))
                    continue;

                var anchored = context.Map.GetAnchoredEntitiesEnumerator(map, grid, indices);
                while (anchored.MoveNext(out var ent))
                {
                    if (whitelist.IsValid(Whitelist, ent.Value))
                        candidates.Add(ent.Value);
                }
            }
        }

        // Candidates come out in grid order; shuffling keeps the picks seed-dependent but order-independent.
        random.Shuffle(candidates);

        if (Count is { } count)
        {
            for (var i = 0; i < Math.Min(count, candidates.Count); i++)
            {
                Replace(entMan, candidates[i]);
            }
            return;
        }

        if (EntitiesPerReplacement is not { } average)
            throw new InvalidOperationException($"{nameof(ReplaceEntities)} needs either {nameof(Count)} or {nameof(EntitiesPerReplacement)}.");

        var prob = Math.Clamp(1 / random.NextGaussian(average, EntitiesPerReplacementStdDev), 0, 1);
        foreach (var candidate in candidates)
        {
            if (random.Prob((float) prob))
                Replace(entMan, candidate);
        }
    }

    private void Replace(IEntityManager entMan, EntityUid target)
    {
        var xform = entMan.GetComponent<TransformComponent>(target);
        var coords = xform.Coordinates;
        var rotation = xform.LocalRotation;

        // Deleted immediately: later effects in the same pass must not pick it up as a candidate.
        entMan.DeleteEntity(target);
        entMan.SpawnAttachedTo(Replacement, coords, rotation: rotation);
    }
}
