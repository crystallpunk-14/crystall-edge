using Content.Shared._CE.Workbench.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server._CE.Workbench;

/// <summary>
/// Stores the set of workbench recipes that this entity (player mob) has learned.
/// Add this component to an entity to allow recipe knowledge tracking.
/// </summary>
[RegisterComponent]
public sealed partial class CERecipeKnowledgeComponent : Component
{
    /// <summary>
    /// Set of recipe prototype IDs that this entity knows how to craft.
    /// </summary>
    [DataField]
    public HashSet<ProtoId<CEWorkbenchRecipePrototype>> KnownRecipes = new();
}
