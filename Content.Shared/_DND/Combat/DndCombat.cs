using Content.Shared._DND.Abilities;

namespace Content.Shared._DND.Combat;

public enum DndAttackOutcome
{
    Miss,
    Hit,
    CriticalHit,
}

/// <summary>
/// Attack rolls, damage, reach, and hit points. One tile is 5 feet.
/// </summary>
public static class DndCombat
{
    public const int DieSides = 20;
    public const int NaturalCritical = 20;
    public const int NaturalMiss = 1;
    public const int FeetPerTile = 5;
    public const int DefaultArmorClass = 10;

    public static int AttackScore(DndAttackAbility ability, int strength, int dexterity)
    {
        switch (ability)
        {
            case DndAttackAbility.Dexterity:
                return dexterity;
            case DndAttackAbility.Finesse:
                return Math.Max(strength, dexterity);
            default:
                return strength;
        }
    }

    public static int AttackBonus(int abilityScore, int proficiencyBonus)
    {
        return DndAbilityScores.Modifier(abilityScore) + proficiencyBonus;
    }

    /// <summary>
    /// A natural 20 hits, and a natural 1 misses, whatever the bonus and the target's AC are.
    /// Any other roll hits when the total is at least the AC.
    /// </summary>
    public static DndAttackOutcome ResolveAttack(int naturalRoll, int attackBonus, int armorClass)
    {
        if (naturalRoll == NaturalCritical)
            return DndAttackOutcome.CriticalHit;

        if (naturalRoll == NaturalMiss)
            return DndAttackOutcome.Miss;

        if (naturalRoll + attackBonus >= armorClass)
            return DndAttackOutcome.Hit;

        return DndAttackOutcome.Miss;
    }

    /// <summary>
    /// A critical hit rolls the damage dice twice. A fixed amount has no dice to double.
    /// </summary>
    public static int DamageDiceCount(int diceCount, bool critical)
    {
        if (diceCount <= 0)
            return 0;

        return critical ? diceCount * 2 : diceCount;
    }

    /// <summary>
    /// Modifiers apply first, then one Resistance (halve, round down), then one Vulnerability (double).
    /// Immunity deals nothing. Extra copies of the same resistance or vulnerability are not passed in.
    /// Damage does not go below 0.
    /// </summary>
    public static int ApplyDamage(
        int rolled,
        int abilityModifier,
        bool addAbilityModifier,
        int adjustment,
        bool resistant,
        bool vulnerable,
        bool immune)
    {
        if (immune)
            return 0;

        var amount = rolled + adjustment;
        if (addAbilityModifier)
            amount += abilityModifier;

        if (amount < 0)
            amount = 0;

        if (resistant)
            amount /= 2;

        if (vulnerable)
            amount *= 2;

        return amount;
    }

    public static bool Listed(List<DndDamageType> types, DndDamageType type)
    {
        foreach (var entry in types)
        {
            if (entry == type)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Level 1 is the hit die's maximum plus Constitution. Each later level adds the die's average, rounded up, plus Constitution.
    /// Each level adds at least 1.
    /// </summary>
    public static int HitPoints(int hitDieSides, int constitutionScore, int level)
    {
        var sides = Math.Max(1, hitDieSides);
        var constitution = DndAbilityScores.Modifier(constitutionScore);
        var levels = Math.Clamp(level, DndAbilityScores.StartingLevel, DndAbilityScores.MaxLevel);
        var first = Math.Max(1, sides + constitution);
        if (levels == 1)
            return first;

        var perLevel = Math.Max(1, sides / 2 + 1 + constitution);
        return first + (levels - 1) * perLevel;
    }

    public static float Tiles(int feet)
    {
        return feet / (float) FeetPerTile;
    }

    /// <summary>
    /// Melee rays travel twice the range stored on the weapon. A 5-foot reach is one tile, so the stored range is half a tile.
    /// </summary>
    public static float MeleeStoredRange(int feet)
    {
        return Tiles(feet) / 2f;
    }
}
