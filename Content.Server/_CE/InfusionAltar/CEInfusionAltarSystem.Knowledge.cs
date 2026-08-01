using Content.Server._CE.InfusionAltar.Components;
using Content.Shared._CE.InfusionAltar;
using Content.Shared._CE.InfusionAltar.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server._CE.InfusionAltar;

public sealed partial class CEInfusionAltarSystem
{
    private void InitKnowledge()
    {
        SubscribeNetworkEvent<CERequestInfusionAltarKnownRecipesEvent>(OnRequestKnownRecipes);
    }

    private void OnRequestKnownRecipes(CERequestInfusionAltarKnownRecipesEvent ev, EntitySessionEventArgs args)
    {
        var recipes = new List<CEInfusionAltarKnownRecipeInfo>();

        if (TryGetSingleton(out var singleton))
        {
            var player = args.SenderSession.AttachedEntity;

            foreach (var recipe in _proto.EnumeratePrototypes<CEInfusionAltarRecipePrototype>())
            {
                if (!singleton.Recipes.TryGetValue(recipe.ID, out var cache))
                    continue;

                // RoundStart recipes are known to everyone unconditionally, checked before anything
                // player-specific (and even without an attached player entity at all).
                if (!recipe.RoundStart && (player is not { } p || !KnowsRecipe(p, recipe.ID)))
                    continue;

                // Cache.PedestalRequirements holds the exact same CEResourceRequirement instances as the
                // recipe's PedestalRequirementPool (RollPedestalRequirements never clones them), so
                // reference equality reliably recovers each rolled entry's pool index.
                var indices = new List<int>(cache.PedestalRequirements.Count);
                foreach (var requirement in cache.PedestalRequirements)
                {
                    var index = recipe.PedestalRequirementPool.FindIndex(entry => ReferenceEquals(entry.Requirement, requirement));
                    if (index >= 0)
                        indices.Add(index);
                }

                recipes.Add(new CEInfusionAltarKnownRecipeInfo(recipe.ID, cache.Essences, indices));
            }
        }

        RaiseNetworkEvent(new CEUpdateInfusionAltarKnownRecipesEvent(recipes), args.SenderSession);
    }

    /// <summary>
    /// Adds a recipe to the entity's known infusion altar recipes.
    /// </summary>
    /// <returns>True if the recipe was newly added; false if already known or component missing.</returns>
    public bool TryAddRecipe(EntityUid target,
        ProtoId<CEInfusionAltarRecipePrototype> recipe,
        CEInfusionAltarRecipeKnowledgeComponent? component = null)
    {
        component ??= EnsureComp<CEInfusionAltarRecipeKnowledgeComponent>(target);

        return component.KnownRecipes.Add(recipe);
    }

    /// <summary>
    /// Removes a recipe from the entity's known infusion altar recipes.
    /// </summary>
    /// <returns>True if the recipe was removed; false if not known or component missing.</returns>
    public bool TryRemoveRecipe(EntityUid target,
        ProtoId<CEInfusionAltarRecipePrototype> recipe,
        CEInfusionAltarRecipeKnowledgeComponent? component = null)
    {
        if (!Resolve(target, ref component, false))
            return false;

        return component.KnownRecipes.Remove(recipe);
    }

    /// <summary>
    /// Checks whether the entity has been taught a specific infusion altar recipe. Does not consider
    /// <see cref="CEInfusionAltarRecipePrototype.RoundStart"/> - callers should check that separately.
    /// Returns false if the entity has no <see cref="CEInfusionAltarRecipeKnowledgeComponent"/>.
    /// </summary>
    public bool KnowsRecipe(EntityUid target,
        ProtoId<CEInfusionAltarRecipePrototype> recipe,
        CEInfusionAltarRecipeKnowledgeComponent? component = null)
    {
        if (!Resolve(target, ref component, false))
            return false; // No knowledge component = knows nothing

        return component.KnownRecipes.Contains(recipe);
    }
}
