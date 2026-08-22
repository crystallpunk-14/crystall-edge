using Content.Shared._CE.MagicEssence.Prototypes;
using Content.Shared._CE.ResourceManager;
using Content.Shared._CE.Skill.Prototypes;
using Content.Shared.Destructible.Thresholds;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.InfusionAltar.Prototypes;

[Prototype("infusionAltarRecipe")]
public sealed partial class CEInfusionAltarRecipePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// The item that must be placed on the altar's central pedestal to identify this recipe.
    /// Consumed on completion, together with everything else the recipe requires.
    /// </summary>
    [DataField(required: true)]
    public CEResourceRequirement Catalyst = default!;

    /// <summary>
    /// Relative weights used to distribute this recipe's rolled essence amount (<see cref="EssenceAmount"/>)
    /// across essence types. Rolled once per round; see <see cref="Content.Server._CE.InfusionAltar.CEInfusionAltarSystem"/>.
    /// </summary>
    [DataField(required: true)]
    public Dictionary<ProtoId<CEMagicEssenceTypePrototype>, int> EssenceWeights = new();

    /// <summary>
    /// Total amount of essence points (summed across all types) this recipe requires. Rolled once per
    /// round, then distributed across <see cref="EssenceWeights"/> by weighted random.
    /// </summary>
    [DataField(required: true)]
    public MinMax EssenceAmount;

    /// <summary>
    /// Weighted pool of possible sub-pedestal requirements. Rolled (with replacement, so entries can
    /// repeat) <see cref="PedestalCount"/> times once per round; see
    /// <see cref="Content.Server._CE.InfusionAltar.CEInfusionAltarSystem"/>.
    /// </summary>
    [DataField(required: true)]
    public List<CEWeightedResourceRequirement> PedestalRequirementPool = new();

    /// <summary>
    /// How many sub-pedestal requirements to roll from <see cref="PedestalRequirementPool"/> this round.
    /// </summary>
    [DataField(required: true)]
    public MinMax PedestalCount;

    [DataField(required: true)]
    public EntProtoId Result;

    [DataField]
    public int ResultCount = 1;

    /// <summary>
    /// Base instability growth rate (per second) while this recipe's ritual is actively progressing
    /// (catalyst identified, all conditions currently satisfied). Represents the recipe's inherent risk,
    /// separate from the extra growth incurred while conditions are unmet.
    /// </summary>
    [DataField]
    public float Instability;

    /// <summary>
    /// How long the recipe's conditions must hold continuously (once fully satisfied) before the craft
    /// completes. Progress pauses (does not reset) whenever a condition is currently unmet.
    /// </summary>
    [DataField]
    public TimeSpan RitualDuration = TimeSpan.Zero;

    /// <summary>
    /// Skill the character must know for this recipe to appear in the guidebook. If unset,
    /// the recipe is always shown. Purely informational - this never gates actually crafting at
    /// the altar, which is discovered through experimentation with catalysts, not taught.
    /// </summary>
    [DataField]
    public ProtoId<CESkillPrototype>? RequiredSkill;
}

/// <summary>
/// A <see cref="CEResourceRequirement"/> paired with a relative weight, for weighted-random rolls.
/// </summary>
[DataDefinition]
public sealed partial class CEWeightedResourceRequirement
{
    [DataField(required: true)]
    public CEResourceRequirement Requirement = default!;

    [DataField]
    public int Weight = 1;
}