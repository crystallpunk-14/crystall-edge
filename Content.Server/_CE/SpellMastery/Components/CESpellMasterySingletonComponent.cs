using Content.Shared._CE.MagicEssence.Prototypes;
using Content.Shared._CE.ResourceManager;
using Content.Shared._CE.SpellMastery.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server._CE.SpellMastery.Components;

// CrystallEdge: rough copy of Content.Server._CE.InfusionAltar.Components.CEInfusionAltarSingletonComponent,
// not yet cleaned up/unified.

[RegisterComponent]
public sealed partial class CESpellMasterySingletonComponent : Component
{
    /// <summary>
    /// Stores per-round unique generated spell mastery ritual recipes
    /// </summary>
    [ViewVariables]
    public Dictionary<ProtoId<CESpellMasteryRitualRecipePrototype>, CESpellMasteryRecipeCache> Recipes = new();
}

[DataDefinition]
public sealed partial class CESpellMasteryRecipeCache
{
    [DataField]
    public Dictionary<ProtoId<CEMagicEssenceTypePrototype>, int> Essences = new();

    [DataField]
    public List<CEResourceRequirement> PedestalRequirements = new();
}
