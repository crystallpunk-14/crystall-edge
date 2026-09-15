using System.Threading.Tasks;
using Content.Server._CE.Demiplane.Prototypes;
using Content.Server._CE.Procedural;
using Content.Server._CE.Procedural.Generation;
using Content.Shared.Tag;
using Robust.Shared.Prototypes;

namespace Content.Server._CE.Demiplane.Generation;

/// <summary>
/// Generates a stage as a stack of maps, one per key in <see cref="LayersByHeight"/>, running each
/// key's <see cref="ICEProceduralLayer"/>s over its map. Modifiers apply themselves afterward. The
/// heavy lifting for the base stack lives in <see cref="CEDungeonSystem.GenerateLayers"/>.
/// </summary>
public sealed partial class LayeredWorldGenerator : ICEDemiplaneLocationGenerator
{
    /// <summary>
    /// Layers grouped by stack height. The key is an ordering label — higher = higher up the stack
    /// (nearest the demiplane entry point) — not a physical index, so gaps just mean "no level there".
    /// </summary>
    [DataField(required: true)]
    public Dictionary<int, List<ICEProceduralLayer>> LayersByHeight = new();

    /// <summary>
    /// Per-category budget for modifier selection. Empty = no modifiers ever get picked.
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<CEDemiplaneModifierCategoryPrototype>, float> ModifierBudget = new();

    /// <summary>
    /// This location's own tags, checked against each candidate modifier's RequiredTags.
    /// </summary>
    [DataField]
    public List<ProtoId<TagPrototype>> Tags = new();

    public async Task<CEDemiplaneGenerationResult> Generate(CEProceduralGenerationContext context)
    {
        var mapsByHeight = await context.EntityManager.System<CEDungeonSystem>().GenerateLayers(context, LayersByHeight);

        var components = new ComponentRegistry();
        foreach (var modifier in SelectModifiers(context))
        {
            foreach (var effect in modifier.Effects)
            {
                await effect.Apply(context, mapsByHeight, components);
            }
        }

        var heights = new List<int>(mapsByHeight.Keys);
        heights.Sort();
        heights.Reverse();

        var maps = new List<EntityUid>(heights.Count);
        foreach (var height in heights)
        {
            maps.Add(mapsByHeight[height]);
        }

        return new CEDemiplaneGenerationResult { Maps = maps, Components = components };
    }

    /// <summary>
    /// Weighted-random pick against <see cref="ModifierBudget"/>, seeded off the run's own seed so
    /// selection is reproducible from it.
    /// </summary>
    private List<CEDemiplaneModifierPrototype> SelectModifiers(CEProceduralGenerationContext context)
    {
        var selected = new List<CEDemiplaneModifierPrototype>();
        if (ModifierBudget.Count == 0)
            return selected;

        var random = new Random(context.Seed);

        var candidates = new Dictionary<CEDemiplaneModifierPrototype, float>();
        foreach (var modifier in context.Prototype.EnumeratePrototypes<CEDemiplaneModifierPrototype>())
        {
            if (context.Difficulty < modifier.Difficulty.Min || context.Difficulty > modifier.Difficulty.Max)
                continue;

            if (random.NextSingle() > modifier.GenerationProb)
                continue;

            var tagsOk = true;
            foreach (var required in modifier.RequiredTags)
            {
                if (!Tags.Contains(required))
                {
                    tagsOk = false;
                    break;
                }
            }

            if (tagsOk)
                candidates[modifier] = modifier.GenerationWeight;
        }

        var remaining = new Dictionary<ProtoId<CEDemiplaneModifierCategoryPrototype>, float>(ModifierBudget);

        while (candidates.Count > 0)
        {
            var picked = WeightedPick(candidates, random);

            var fits = true;
            foreach (var (category, cost) in picked.Categories)
            {
                if (!remaining.TryGetValue(category, out var left) || left - cost < 0)
                {
                    fits = false;
                    break;
                }
            }

            if (!fits)
            {
                candidates.Remove(picked);
                continue;
            }

            selected.Add(picked);
            foreach (var (category, cost) in picked.Categories)
            {
                remaining[category] -= cost;
            }

            if (picked.Unique)
                candidates.Remove(picked);
        }

        return selected;
    }

    private static CEDemiplaneModifierPrototype WeightedPick(Dictionary<CEDemiplaneModifierPrototype, float> weights, Random random)
    {
        var total = 0f;
        foreach (var weight in weights.Values)
        {
            total += weight;
        }

        var roll = (float)random.NextDouble() * total;
        var last = default(CEDemiplaneModifierPrototype)!;
        foreach (var (modifier, weight) in weights)
        {
            last = modifier;
            roll -= weight;
            if (roll <= 0f)
                return modifier;
        }

        return last;
    }
}
