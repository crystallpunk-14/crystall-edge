using Content.Shared._CE.InfusionAltar.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.EntityEffect.Effects;

/// <summary>
/// Teaches the target entity a set of infusion altar recipes.
/// Server-side logic is handled by <c>CELearnInfusionRecipeEffectSystem</c>.
/// </summary>
public sealed partial class LearnInfusionRecipe : CEEntityEffectBase<LearnInfusionRecipe>
{
    public LearnInfusionRecipe()
    {
        EffectTarget = CEEffectTarget.Target;
    }

    /// <summary>
    /// Recipes to teach to the target.
    /// </summary>
    [DataField(required: true)]
    public List<ProtoId<CEInfusionAltarRecipePrototype>> Recipes = new();

    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        var lines = new List<string>();
        foreach (var recipeId in Recipes)
        {
            if (!prototype.TryIndex(recipeId, out var recipe))
                continue;

            var itemName = prototype.TryIndex(recipe.Result, out var itemProto) ? itemProto.Name : recipe.Result.Id;
            lines.Add(Loc.GetString("ce-entity-effect-guidebook-learn-infusion-recipe", ("item", itemName)));
        }

        return lines.Count > 0 ? string.Join("\n", lines) : base.EntityEffectGuidebookText(prototype, entSys);
    }
}
