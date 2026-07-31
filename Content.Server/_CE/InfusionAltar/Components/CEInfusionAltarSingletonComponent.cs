using Content.Shared._CE.InfusionAltar.Prototypes;
using Content.Shared._CE.MagicEssence.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server._CE.InfusionAltar.Components;

/// <summary>
/// Marks the singleton nullspace entity holding round-wide infusion altar recipe data.
/// Spawned by <see cref="CEInfusionAltarSystem"/> on round start; duplicates are deleted on MapInit.
/// Server-only: not networked, since recipe requirements must stay hidden from clients.
/// </summary>
[RegisterComponent]
public sealed partial class CEInfusionAltarSingletonComponent : Component
{
    /// <summary>
    /// Each recipe's rolled requirements, generated once per round by <see cref="CEInfusionAltarSystem.GenerateRecipe"/>.
    /// </summary>
    [ViewVariables]
    public Dictionary<ProtoId<CEInfusionAltarRecipePrototype>, CEInfusionAltarRecipeCache> Recipes = new();
}

/// <summary>
/// Rolled, round-cached requirements for a single infusion altar recipe. Holds essence costs for now;
/// future rolled requirements (surrounding ingredients, instability modifiers, etc.) belong here too,
/// alongside their roll logic in <see cref="CEInfusionAltarSystem.GenerateRecipe"/>.
/// </summary>
[DataDefinition]
public sealed partial class CEInfusionAltarRecipeCache
{
    [DataField]
    public Dictionary<ProtoId<CEMagicEssenceTypePrototype>, int> Essences = new();
}
