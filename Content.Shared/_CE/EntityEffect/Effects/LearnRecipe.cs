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
}
