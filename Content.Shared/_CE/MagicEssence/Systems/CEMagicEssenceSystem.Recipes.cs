using Content.Shared._CE.MagicEssence.Prototypes;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.EntityEffects.Effects.EntitySpawning;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.MagicEssence.Systems;

public sealed partial class CEMagicEssenceSystem
{
    private Dictionary<ProtoId<CEMagicEssenceTypePrototype>, List<ProtoId<CEMagicEssenceTypePrototype>>>? _essenceRecipes;
    private Dictionary<(string, string), ProtoId<CEMagicEssenceTypePrototype>>? _essenceRecipesByComponents;

    /// <summary>
    /// Resolves the lower-tier essences that combine to produce the given essence type, for display in
    /// the thaumaturgy guidebook. Empty for tier 0 (primal) aspects, which aren't produced by any reaction.
    /// </summary>
    public IReadOnlyList<ProtoId<CEMagicEssenceTypePrototype>> GetRecipeComponents(ProtoId<CEMagicEssenceTypePrototype> essence)
    {
        var recipes = GetEssenceRecipeMap();
        return recipes.TryGetValue(essence, out var components) ? components : [];
    }

    /// <summary>
    /// Reverse of <see cref="GetRecipeComponents"/>: given two component aspects (in either order),
    /// resolves the higher-tier aspect they combine into. Used by the knowledge panel's merge feature.
    /// False if no two-ingredient recipe matches this pair.
    /// </summary>
    public bool TryGetMergeResult(
        ProtoId<CEMagicEssenceTypePrototype> a,
        ProtoId<CEMagicEssenceTypePrototype> b,
        out ProtoId<CEMagicEssenceTypePrototype> result)
    {
        var key = string.CompareOrdinal(a.Id, b.Id) <= 0 ? (a.Id, b.Id) : (b.Id, a.Id);
        return GetEssenceRecipesByComponents().TryGetValue(key, out result);
    }

    /// <summary>
    /// Inverts <see cref="GetEssenceRecipeMap"/>: for every recipe with exactly 2 components, maps
    /// the unordered pair of their IDs back to the resulting essence.
    /// </summary>
    private Dictionary<(string, string), ProtoId<CEMagicEssenceTypePrototype>> GetEssenceRecipesByComponents()
    {
        if (_essenceRecipesByComponents is { } cached)
            return cached;

        var map = new Dictionary<(string, string), ProtoId<CEMagicEssenceTypePrototype>>();
        foreach (var (essence, components) in GetEssenceRecipeMap())
        {
            if (components.Count != 2)
                continue;

            var key = string.CompareOrdinal(components[0].Id, components[1].Id) <= 0
                ? (components[0].Id, components[1].Id)
                : (components[1].Id, components[0].Id);

            map[key] = essence;
        }

        _essenceRecipesByComponents = map;
        return map;
    }

    private Dictionary<ProtoId<CEMagicEssenceTypePrototype>, List<ProtoId<CEMagicEssenceTypePrototype>>> GetEssenceRecipeMap()
    {
        if (_essenceRecipes is { } cached)
            return cached;

        var reagentToEssence = GetReagentEssenceMap();

        // Entity -> the reagent that is that same essence's own pure liquid embodiment, so
        // self-producing reactions (e.g. evaporation: an essence reagent that just re-spawns itself)
        // can be told apart below from genuine combining recipes that also spawn that entity.
        var entityOwnReagent = new Dictionary<EntProtoId, ProtoId<ReagentPrototype>>();
        foreach (var essenceType in _proto.EnumeratePrototypes<CEMagicEssenceTypePrototype>())
        {
            if (essenceType.EssenceProto is { } ownEnt && essenceType.Reagent is { } ownReagent)
                entityOwnReagent[ownEnt] = ownReagent;
        }

        var entityToReaction = new Dictionary<EntProtoId, ReactionPrototype>();
        foreach (var reaction in _proto.EnumeratePrototypes<ReactionPrototype>())
        {
            foreach (var effect in reaction.Effects)
            {
                if (effect is not SpawnEntity spawn)
                    continue;

                // Not a combining recipe - it produces the essence from itself (e.g. evaporation).
                // Multiple reactions can spawn the same essence entity, so without this check
                // whichever happens to enumerate last would silently clobber the real recipe here.
                if (entityOwnReagent.TryGetValue(spawn.Entity, out var ownReagent) && reaction.Reactants.ContainsKey(ownReagent))
                    continue;

                entityToReaction[spawn.Entity] = reaction;
            }
        }

        var map = new Dictionary<ProtoId<CEMagicEssenceTypePrototype>, List<ProtoId<CEMagicEssenceTypePrototype>>>();
        foreach (var essenceType in _proto.EnumeratePrototypes<CEMagicEssenceTypePrototype>())
        {
            if (essenceType.EssenceProto is not { } essenceEnt || !entityToReaction.TryGetValue(essenceEnt, out var reaction))
                continue;

            var components = new List<ProtoId<CEMagicEssenceTypePrototype>>();
            foreach (var reagentId in reaction.Reactants.Keys)
            {
                if (reagentToEssence.TryGetValue(reagentId, out var component))
                    components.Add(component);
                else
                    Log.Warning($"Thaumaturgy guidebook: recipe reaction \"{reaction.ID}\" for essence \"{essenceType.ID}\" has reactant reagent \"{reagentId}\" with no matching magicEssenceType.");
            }

            if (components.Count > 0)
                map[essenceType.ID] = components;
        }

        _essenceRecipes = map;
        return map;
    }
}
