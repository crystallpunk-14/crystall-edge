using Robust.Shared.Prototypes;

namespace Content.Shared._DND.Classes;

public enum DndFeatureKind
{
    Rage,
    UnarmoredDefense,
    WeaponMastery,
    BardicInspiration,
    Spellcasting,
    DivineOrder,
    Druidic,
    PrimalOrder,
}

/// <summary>
/// A class feature gained at a level. The numbers live on the prototype so each class file can carry its own table.
/// </summary>
[Prototype("dndFeature")]
public sealed partial class DndFeaturePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public LocId Name { get; private set; }

    [DataField(required: true)]
    public LocId Description { get; private set; }

    /// <summary>
    /// The class level at which this feature is gained.
    /// </summary>
    [DataField]
    public int Level { get; private set; } = 1;

    [DataField(required: true)]
    public DndFeatureKind Kind { get; private set; }

    /// <summary>
    /// Rage uses at each level. Index 0 is level 1.
    /// </summary>
    [DataField]
    public List<int> UsesByLevel { get; private set; } = new();

    /// <summary>
    /// Extra damage at each level. Index 0 is level 1.
    /// </summary>
    [DataField]
    public List<int> DamageByLevel { get; private set; } = new();

    /// <summary>
    /// Weapon kinds whose mastery you can use, at each level. Index 0 is level 1.
    /// </summary>
    [DataField]
    public List<int> MasteryByLevel { get; private set; } = new();

    /// <summary>
    /// Bardic Inspiration die at each level. Index 0 is level 1.
    /// </summary>
    [DataField]
    public List<int> DieByLevel { get; private set; } = new();

    /// <summary>
    /// Spell slots at each level. Index 0 is level 1.
    /// </summary>
    [DataField]
    public List<int> SlotsByLevel { get; private set; } = new();
}
