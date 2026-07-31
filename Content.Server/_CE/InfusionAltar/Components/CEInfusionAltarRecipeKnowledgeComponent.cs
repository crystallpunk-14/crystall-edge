using Content.Shared._CE.InfusionAltar.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server._CE.InfusionAltar.Components;

/// <summary>
/// Stores the set of infusion altar recipes that this entity (player mob) has been taught, via
/// <see cref="Content.Shared._CE.EntityEffect.Effects.LearnInfusionRecipe"/>. Add this component to an
/// entity to allow recipe knowledge tracking.
/// </summary>
[RegisterComponent]
public sealed partial class CEInfusionAltarRecipeKnowledgeComponent : Component
{
    /// <summary>
    /// Set of recipe prototype IDs that this entity has been taught.
    /// </summary>
    [DataField]
    public HashSet<ProtoId<CEInfusionAltarRecipePrototype>> KnownRecipes = new();
}
