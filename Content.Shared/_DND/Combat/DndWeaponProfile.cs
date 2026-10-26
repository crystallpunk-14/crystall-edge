using Content.Shared._DND.Equipment;

namespace Content.Shared._DND.Combat;

/// <summary>
/// The dice, ability, and reach used when an attack is rolled.
/// </summary>
public readonly struct DndWeaponProfile
{
    public int DiceCount { get; init; }
    public int DiceSides { get; init; }
    public int FixedDamage { get; init; }
    public bool AddAbilityModifier { get; init; }
    public DndDamageType DamageType { get; init; }
    public DndAttackAbility Ability { get; init; }
    public int RangeFeet { get; init; }
    public bool Ranged { get; init; }
    public string? Kind { get; init; }
    public DndWeaponTraining? Training { get; init; }
    public DndWeaponMastery Mastery { get; init; }
    public bool Light { get; init; }
    public bool Improvised { get; init; }

    public static DndWeaponProfile Unarmed => new()
    {
        DiceCount = 0,
        FixedDamage = 1,
        AddAbilityModifier = true,
        DamageType = DndDamageType.Bludgeoning,
        Ability = DndAttackAbility.Strength,
        RangeFeet = 5,
    };

    public static DndWeaponProfile From(DndWeaponComponent weapon)
    {
        return new DndWeaponProfile
        {
            DiceCount = weapon.DiceCount,
            DiceSides = weapon.DiceSides,
            FixedDamage = weapon.FixedDamage,
            AddAbilityModifier = weapon.AddAbilityModifier,
            DamageType = weapon.DamageType,
            Ability = weapon.Ability,
            RangeFeet = weapon.RangeFeet,
            Ranged = weapon.Ranged,
            Kind = weapon.Kind,
            Training = weapon.Training,
            Mastery = weapon.Mastery,
            Light = weapon.Light,
            Improvised = weapon.Improvised,
        };
    }
}
