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
}
