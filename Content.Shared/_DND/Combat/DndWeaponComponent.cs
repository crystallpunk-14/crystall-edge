using Content.Shared._DND.Equipment;

namespace Content.Shared._DND.Combat;

/// <summary>
/// Damage dice and reach for a weapon. Range is in feet. One tile is 5 feet.
/// A fixed amount with no dice does not add an ability modifier unless <see cref="AddAbilityModifier"/> says the rule does.
/// </summary>
[RegisterComponent]
public sealed partial class DndWeaponComponent : Component
{
    [DataField]
    public int DiceCount = 1;

    [DataField]
    public int DiceSides = 4;

    [DataField]
    public int FixedDamage;

    [DataField]
    public bool AddAbilityModifier = true;

    [DataField]
    public DndDamageType DamageType = DndDamageType.Bludgeoning;

    [DataField]
    public DndAttackAbility Ability = DndAttackAbility.Strength;

    [DataField]
    public int RangeFeet = 5;

    [DataField]
    public bool Ranged;

    /// <summary>
    /// Weapon kind, such as Greataxe. Empty for an unarmed strike or an improvised weapon.
    /// </summary>
    [DataField]
    public string? Kind;

    [DataField]
    public DndWeaponTraining? Training;

    [DataField]
    public DndWeaponMastery Mastery;

    [DataField]
    public bool Light;

    /// <summary>
    /// An improvised weapon does not add proficiency.
    /// </summary>
    [DataField]
    public bool Improvised;
}
