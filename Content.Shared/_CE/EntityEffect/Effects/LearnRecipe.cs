using Content.Shared._CE.Workbench.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.EntityEffect.Effects;

/// <summary>
/// Teaches the target entity a set of workbench recipes.
/// Server-side logic is handled by <c>CELearnRecipeEffectSystem</c>.
/// </summary>
public sealed partial class LearnRecipe : CEEntityEffectBase<LearnRecipe>
{
    public LearnRecipe()
    {
        EffectTarget = CEEffectTarget.Target;
    }

    /// <summary>
    /// Recipes to teach to the target.
    /// </summary>
    [DataField(required: true)]
    public List<ProtoId<CEWorkbenchRecipePrototype>> Recipes = new();

    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        var lines = new List<string>();
        foreach (var recipeId in Recipes)
        {
            if (!prototype.TryIndex(recipeId, out var recipe))
                continue;

            var itemName = prototype.TryIndex(recipe.Result, out var itemProto)
                ? itemProto.Name
                : recipe.Result.Id;

            var workbenchName = recipe.Workbench is { } workbenchId && prototype.TryIndex(workbenchId, out var workbenchProto)
                ? workbenchProto.Name
                : Loc.GetString("ce-entity-effect-guidebook-learn-recipe-unknown-workbench");

            lines.Add(Loc.GetString("ce-entity-effect-guidebook-learn-recipe", ("item", itemName), ("workbench", workbenchName)));
        }

        return lines.Count > 0 ? string.Join("\n", lines) : base.EntityEffectGuidebookText(prototype, entSys);
    }
}
