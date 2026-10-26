using Content.Shared._DND.Equipment;
using Robust.Shared.Prototypes;

namespace Content.Shared._DND.Combat;

/// <summary>
/// Maps the weapons already in the world onto damage dice, attack ability, and reach in feet.
/// </summary>
public static class DndWeaponCatalog
{
    public static void Fill(
        IPrototypeManager prototypes,
        EntityPrototype proto,
        DndWeaponComponent weapon,
        bool unarmed,
        bool rangedFallback)
    {
        if (unarmed)
        {
            Apply(weapon, DndWeaponProfile.Unarmed);
            return;
        }

        if (proto.ID == "CEWeaponDaggerHatchet")
        {
            Melee(weapon, 1, 6, DndDamageType.Slashing, DndAttackAbility.Strength, "Handaxe", DndWeaponMastery.Vex, DndWeaponTraining.Simple, light: true);
            return;
        }

        if (proto.ID == "CEWeaponDaggerSickle")
        {
            Melee(weapon, 1, 4, DndDamageType.Slashing, DndAttackAbility.Strength, "Sickle", DndWeaponMastery.Nick, DndWeaponTraining.Simple, light: true);
            return;
        }

        if (IsA(prototypes, proto, "CEBaseWeaponDagger"))
        {
            Melee(weapon, 1, 4, DndDamageType.Piercing, DndAttackAbility.Finesse, "Dagger", DndWeaponMastery.Nick, DndWeaponTraining.Simple, light: true);
            return;
        }

        if (IsA(prototypes, proto, "CEBaseWeaponSpear"))
        {
            Melee(weapon, 1, 6, DndDamageType.Piercing, DndAttackAbility.Strength, "Spear", DndWeaponMastery.Sap, DndWeaponTraining.Simple);
            return;
        }

        if (IsA(prototypes, proto, "CEBaseWeaponTwoHandedSword"))
        {
            Melee(weapon, 2, 6, DndDamageType.Slashing, DndAttackAbility.Strength, "Greatsword", DndWeaponMastery.Graze, DndWeaponTraining.Martial);
            return;
        }

        if (IsA(prototypes, proto, "CEBaseWeaponSkimitar"))
        {
            Melee(weapon, 1, 6, DndDamageType.Slashing, DndAttackAbility.Finesse, "Scimitar", DndWeaponMastery.Nick, DndWeaponTraining.Martial, light: true);
            return;
        }

        if (IsA(prototypes, proto, "CEBaseWeaponSword"))
        {
            Melee(weapon, 1, 8, DndDamageType.Slashing, DndAttackAbility.Strength, "Longsword", DndWeaponMastery.Sap, DndWeaponTraining.Martial);
            return;
        }

        if (IsA(prototypes, proto, "CEBaseWeaponWarAxe"))
        {
            Melee(weapon, 1, 12, DndDamageType.Slashing, DndAttackAbility.Strength, "Greataxe", DndWeaponMastery.Cleave, DndWeaponTraining.Martial);
            return;
        }

        if (IsA(prototypes, proto, "CEBaseWeaponMaceTwoHanded"))
        {
            Melee(weapon, 2, 6, DndDamageType.Bludgeoning, DndAttackAbility.Strength, "Maul", DndWeaponMastery.Topple, DndWeaponTraining.Martial);
            return;
        }

        if (IsA(prototypes, proto, "CEBaseWeaponStaff"))
        {
            Melee(weapon, 1, 6, DndDamageType.Bludgeoning, DndAttackAbility.Strength, "Quarterstaff", DndWeaponMastery.Topple, DndWeaponTraining.Simple);
            return;
        }

        if (IsA(prototypes, proto, "CEBaseWeaponPickaxe"))
        {
            Melee(weapon, 1, 8, DndDamageType.Piercing, DndAttackAbility.Strength, "WarPick", DndWeaponMastery.Sap, DndWeaponTraining.Martial);
            return;
        }

        if (IsA(prototypes, proto, "CEBaseWeaponHammer"))
        {
            Melee(weapon, 1, 4, DndDamageType.Bludgeoning, DndAttackAbility.Strength, "LightHammer", DndWeaponMastery.Nick, DndWeaponTraining.Simple, light: true);
            return;
        }

        if (IsA(prototypes, proto, "CEBaseWeaponClub"))
        {
            Melee(weapon, 1, 4, DndDamageType.Bludgeoning, DndAttackAbility.Strength, "Club", DndWeaponMastery.Slow, DndWeaponTraining.Simple);
            return;
        }

        if (IsA(prototypes, proto, "CEBaseWeaponShovel") || IsA(prototypes, proto, "CEBaseWeaponMop"))
        {
            Melee(weapon, 1, 4, DndDamageType.Bludgeoning, DndAttackAbility.Strength, improvised: true);
            return;
        }

        if (IsA(prototypes, proto, "CEBaseWeaponBow"))
        {
            Ranged(weapon, 1, 8, 600, DndWeaponTraining.Martial);
            return;
        }

        if (IsA(prototypes, proto, "CEWeaponLightCrossbow"))
        {
            Ranged(weapon, 1, 8, 320, DndWeaponTraining.Simple);
            return;
        }

        if (rangedFallback || IsA(prototypes, proto, "CEWeaponGunPeacekeeper") || IsA(prototypes, proto, "CEWeaponGunJustice"))
        {
            Ranged(weapon, 1, 10, 120);
            return;
        }

        Melee(weapon, 1, 4, DndDamageType.Bludgeoning, DndAttackAbility.Strength, improvised: true);
    }

    private static void Melee(
        DndWeaponComponent weapon,
        int count,
        int sides,
        DndDamageType type,
        DndAttackAbility ability,
        string? kind = null,
        DndWeaponMastery mastery = DndWeaponMastery.None,
        DndWeaponTraining? training = null,
        bool light = false,
        bool improvised = false)
    {
        weapon.DiceCount = count;
        weapon.DiceSides = sides;
        weapon.FixedDamage = 0;
        weapon.AddAbilityModifier = true;
        weapon.DamageType = type;
        weapon.Ability = ability;
        weapon.RangeFeet = 5;
        weapon.Ranged = false;
        weapon.Kind = kind;
        weapon.Training = training;
        weapon.Mastery = mastery;
        weapon.Light = light;
        weapon.Improvised = improvised;
    }

    private static void Ranged(DndWeaponComponent weapon, int count, int sides, int rangeFeet, DndWeaponTraining? training = null)
    {
        weapon.DiceCount = count;
        weapon.DiceSides = sides;
        weapon.FixedDamage = 0;
        weapon.AddAbilityModifier = true;
        weapon.DamageType = DndDamageType.Piercing;
        weapon.Ability = DndAttackAbility.Dexterity;
        weapon.RangeFeet = rangeFeet;
        weapon.Ranged = true;
        weapon.Kind = null;
        weapon.Training = training;
        weapon.Mastery = DndWeaponMastery.None;
        weapon.Light = false;
        weapon.Improvised = training == null;
    }

    private static void Apply(DndWeaponComponent weapon, DndWeaponProfile profile)
    {
        weapon.DiceCount = profile.DiceCount;
        weapon.DiceSides = profile.DiceSides;
        weapon.FixedDamage = profile.FixedDamage;
        weapon.AddAbilityModifier = profile.AddAbilityModifier;
        weapon.DamageType = profile.DamageType;
        weapon.Ability = profile.Ability;
        weapon.RangeFeet = profile.RangeFeet;
        weapon.Ranged = profile.Ranged;
        weapon.Kind = profile.Kind;
        weapon.Training = profile.Training;
        weapon.Mastery = profile.Mastery;
        weapon.Light = profile.Light;
        weapon.Improvised = profile.Improvised;
    }

    private static bool IsA(IPrototypeManager prototypes, EntityPrototype proto, string id)
    {
        if (proto.ID == id)
            return true;

        if (proto.Parents == null)
            return false;

        foreach (var parentId in proto.Parents)
        {
            if (parentId == id)
                return true;

            if (prototypes.TryIndex<EntityPrototype>(parentId, out var parent) && IsA(prototypes, parent, id))
                return true;
        }

        return false;
    }
}
