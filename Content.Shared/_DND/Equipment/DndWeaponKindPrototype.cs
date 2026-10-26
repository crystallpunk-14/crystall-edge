using Robust.Shared.Prototypes;

namespace Content.Shared._DND.Equipment;

/// <summary>
/// A kind of weapon, such as Greataxe or Handaxe, and the mastery property that kind carries.
/// </summary>
[Prototype("dndWeaponKind")]
public sealed partial class DndWeaponKindPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public LocId Name { get; private set; }

    [DataField]
    public DndWeaponTraining Training { get; private set; } = DndWeaponTraining.Simple;

    /// <summary>
    /// Melee kinds can be chosen for Weapon Mastery. Ranged kinds still grant proficiency when the class trains them.
    /// </summary>
    [DataField]
    public bool Melee { get; private set; } = true;

    [DataField]
    public DndWeaponMastery Mastery { get; private set; } = DndWeaponMastery.None;

    [DataField]
    public bool Light { get; private set; }
}
