using Content.Shared._CE.InfusionAltar.Prototypes;
using Content.Shared._CE.MagicEssence.Prototypes;
using Content.Shared._CE.ResourceManager;
using Robust.Shared.Prototypes;

namespace Content.Server._CE.InfusionAltar.Components;

[RegisterComponent]
public sealed partial class CEInfusionAltarSingletonComponent : Component
{
    /// <summary>
    /// Stores per-round unique generated infusion altar recipes
    /// </summary>
    [ViewVariables]
    public Dictionary<ProtoId<CEInfusionAltarRecipePrototype>, CEInfusionAltarRecipeCache> Recipes = new();
}

[DataDefinition]
public sealed partial class CEInfusionAltarRecipeCache
{
    [DataField]
    public Dictionary<ProtoId<CEMagicEssenceTypePrototype>, int> Essences = new();

    [DataField]
    public List<CEResourceRequirement> PedestalRequirements = new();
}
