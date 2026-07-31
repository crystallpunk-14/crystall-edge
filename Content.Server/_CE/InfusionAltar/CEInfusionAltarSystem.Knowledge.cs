using Content.Server._CE.InfusionAltar.Components;
using Content.Shared._CE.InfusionAltar.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server._CE.InfusionAltar;

public sealed partial class CEInfusionAltarSystem
{
    /// <summary>
    /// Adds a recipe to the entity's known infusion altar recipes.
    /// </summary>
    /// <returns>True if the recipe was newly added; false if already known or component missing.</returns>
    public bool TryAddRecipe(EntityUid target,
        ProtoId<CEInfusionAltarRecipePrototype> recipe,
        CEInfusionAltarRecipeKnowledgeComponent? component = null)
    {
        if (!Resolve(target, ref component, false))
            return false;

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

    /// <summary>
    /// Returns all infusion altar recipes known by the entity, or null if no knowledge component exists.
    /// </summary>
    public HashSet<ProtoId<CEInfusionAltarRecipePrototype>>? GetKnownRecipes(EntityUid target,
        CEInfusionAltarRecipeKnowledgeComponent? component = null)
    {
        if (!Resolve(target, ref component, false))
            return null;

        return component.KnownRecipes;
    }
}
