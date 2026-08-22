using Content.Shared._CE.InfusionAltar.Prototypes;
using Content.Shared._CE.MagicEssence.Prototypes;
using Content.Shared._CE.Skill.Prototypes;
using Content.Shared.Destructible.Thresholds;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.SpellMastery.Prototypes;

// CrystallEdge: rough copy of Content.Shared._CE.InfusionAltar.Prototypes.CEInfusionAltarRecipePrototype,
// not yet cleaned up/unified. Reuses CEWeightedResourceRequirement from the infusion altar namespace
// as-is (a generic weighted-requirement wrapper, nothing altar-specific about it).

[Prototype("spellMasteryRitualRecipe")]
public sealed partial class CESpellMasteryRitualRecipePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// Theory skill this recipe is associated with. Purely informational, same as
    /// <see cref="CEInfusionAltarRecipePrototype.RequiredSkill"/> - reveals the recipe in the
    /// guidebook, but never gates actually performing the ritual, which is identified by essence and
    /// pedestal contents alone, just like the infusion altar.
    /// </summary>
    [DataField]
    public ProtoId<CESkillPrototype>? RequiredSkill;

    /// <summary>
    /// Relative weights used to distribute this recipe's rolled essence amount (<see cref="EssenceAmount"/>)
    /// across essence types. Rolled once per round; see <see cref="Content.Server._CE.SpellMastery.CESpellMasterySystem"/>.
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
    /// <see cref="Content.Server._CE.SpellMastery.CESpellMasterySystem"/>.
    /// </summary>
    [DataField(required: true)]
    public List<CEWeightedResourceRequirement> PedestalRequirementPool = new();

    /// <summary>
    /// How many sub-pedestal requirements to roll from <see cref="PedestalRequirementPool"/> this round.
    /// </summary>
    [DataField(required: true)]
    public MinMax PedestalCount;

    /// <summary>
    /// The Mastery skill granted to the buckled player on completion (takes the place of the infusion
    /// altar's spawned item Result).
    /// </summary>
    [DataField(required: true)]
    public ProtoId<CESkillPrototype> Result = default!;

    /// <summary>
    /// Base instability growth rate (per second) while this recipe's ritual is actively progressing
    /// (buckled player identified, all conditions currently satisfied). Represents the recipe's
    /// inherent risk, separate from the extra growth incurred while conditions are unmet.
    /// </summary>
    [DataField]
    public float Instability;

    /// <summary>
    /// How long the recipe's conditions must hold continuously (once fully satisfied) before the
    /// mastery completes. Progress pauses (does not reset) whenever a condition is currently unmet.
    /// </summary>
    [DataField]
    public TimeSpan RitualDuration = TimeSpan.Zero;
}
