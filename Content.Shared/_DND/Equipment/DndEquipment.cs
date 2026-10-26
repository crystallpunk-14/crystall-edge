using Robust.Shared.Prototypes;

namespace Content.Shared._DND.Equipment;

public enum DndWeaponTraining
{
    Simple,
    Martial,
}

public enum DndArmorCategory
{
    Light,
    Medium,
    Heavy,
    Shield,
}

/// <summary>
/// Mastery property of one weapon kind. None means the weapon has no mastery.
/// </summary>
public enum DndWeaponMastery
{
    None,
    Cleave,
    Graze,
    Nick,
    Push,
    Sap,
    Slow,
    Topple,
    Vex,
}

public enum DndCreatureSize
{
    Tiny,
    Small,
    Medium,
    Large,
    Huge,
    Gargantuan,
}

[DataDefinition]
public sealed partial class DndEquipmentOption
{
    [DataField("id", required: true)]
    public string ID { get; private set; } = "";

    [DataField(required: true)]
    public LocId Name { get; private set; }

    [DataField]
    public List<DndEquipmentStack> Items { get; private set; } = new();
}

[DataDefinition]
public sealed partial class DndEquipmentStack
{
    [DataField(required: true)]
    public EntProtoId Prototype { get; private set; }

    [DataField]
    public int Amount { get; private set; } = 1;
}
