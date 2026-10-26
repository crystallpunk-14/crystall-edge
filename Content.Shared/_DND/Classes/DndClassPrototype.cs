using Content.Shared._DND.Abilities;
using Content.Shared._DND.Equipment;
using Content.Shared._DND.Skills;
using Content.Shared._DND.Spells;
using Robust.Shared.Prototypes;

namespace Content.Shared._DND.Classes;

/// <summary>
/// A character class chosen instead of a job.
/// </summary>
[Prototype("dndClass")]
public sealed partial class DndClassPrototype : IPrototype
{
    public const string Default = "Fighter";

    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public LocId Name { get; private set; }

    [DataField(required: true)]
    public LocId Description { get; private set; }

    /// <summary>
    /// Display order in character creation. Lower comes first.
    /// </summary>
    [DataField]
    public int Order { get; private set; }

    /// <summary>
    /// Join the round as a ghost and pick a ghost role, instead of spawning a body.
    /// </summary>
    [DataField]
    public bool SpawnAsGhost { get; private set; }

    /// <summary>
    /// Sides on the class hit die. Level 1 uses the maximum. Later levels use the average, rounded up.
    /// </summary>
    [DataField]
    public int HitDie { get; private set; } = 8;

    [DataField]
    public ProtoId<DndAbilityPrototype> PrimaryAbility { get; private set; }

    [DataField]
    public List<ProtoId<DndAbilityPrototype>> SavingThrows { get; private set; } = new();

    [DataField]
    public List<ProtoId<DndSkillPrototype>> SkillOptions { get; private set; } = new();

    [DataField]
    public int SkillChoiceCount { get; private set; }

    /// <summary>
    /// Musical instruments the class can learn. Empty when the class has no tools.
    /// </summary>
    [DataField]
    public List<ProtoId<DndInstrumentPrototype>> InstrumentOptions { get; private set; } = new();

    [DataField]
    public int InstrumentChoiceCount { get; private set; }

    /// <summary>
    /// Which spell list this class prepares from. Empty when the class does not cast.
    /// </summary>
    [DataField]
    public string SpellList { get; private set; } = "";

    [DataField]
    public int CantripChoiceCount { get; private set; }

    [DataField]
    public int PreparedSpellCount { get; private set; }

    /// <summary>
    /// Item that focuses this class's spells when a spell needs one.
    /// </summary>
    [DataField]
    public DndFocusKind SpellFocus { get; private set; } = DndFocusKind.None;

    /// <summary>
    /// A long rest opens a short window to change prepared spells.
    /// </summary>
    [DataField]
    public bool ReprepareOnLongRest { get; private set; }

    /// <summary>
    /// Level 1 spells that are always prepared and do not count against the prepared number.
    /// </summary>
    [DataField]
    public List<string> AlwaysPrepared { get; private set; } = new();

    [DataField]
    public List<DndWeaponTraining> WeaponTraining { get; private set; } = new();

    [DataField]
    public List<DndArmorCategory> ArmorTraining { get; private set; } = new();

    [DataField]
    public List<DndEquipmentOption> EquipmentOptions { get; private set; } = new();

    [DataField]
    public List<ProtoId<DndFeaturePrototype>> Features { get; private set; } = new();

    [DataField]
    public List<ProtoId<DndWeaponKindPrototype>> DefaultMasteries { get; private set; } = new();
}
