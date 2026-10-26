using Content.Shared._DND.Abilities;
using Content.Shared._DND.Combat;
using Content.Shared._DND.Equipment;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared._DND.Features;

/// <summary>
/// Rage timing for real time. Six seconds is one stretch of fighting. Attacking an enemy, or choosing to press on, refreshes that stretch.
/// The rage cannot be kept up longer than ten minutes.
/// </summary>
public static class DndRageRules
{
    public const float RoundSeconds = 6f;
    public const float MaxSeconds = 600f;

    /// <summary>
    /// Ten feet off a thirty-foot pace.
    /// </summary>
    public const float SlowedSpeed = 20f / 30f;

    public static int UnarmoredArmorClass(int dexterity, int constitution)
    {
        return DndAbilityScores.BaseArmorClassValue
            + DndAbilityScores.Modifier(dexterity)
            + DndAbilityScores.Modifier(constitution);
    }

    /// <summary>
    /// A finesse attack uses Strength when Strength is at least Dexterity, so a tie still rages.
    /// </summary>
    public static bool UsesStrength(DndAttackAbility ability, int strength, int dexterity)
    {
        switch (ability)
        {
            case DndAttackAbility.Dexterity:
                return false;
            case DndAttackAbility.Finesse:
                return strength >= dexterity;
            default:
                return true;
        }
    }

    public static bool IsPhysical(DndDamageType type)
    {
        return type is DndDamageType.Bludgeoning or DndDamageType.Piercing or DndDamageType.Slashing;
    }

    public static int Graze(int abilityModifier)
    {
        return Math.Max(0, abilityModifier);
    }

    /// <summary>
    /// Cleave adds the ability modifier only when that modifier is negative.
    /// </summary>
    public static bool CleaveAddsModifier(int abilityModifier)
    {
        return abilityModifier < 0;
    }

    public static bool CanPush(DndCreatureSize size)
    {
        return size <= DndCreatureSize.Large;
    }

    public static TimeSpan RefreshExpiry(TimeSpan now, TimeSpan maxEnd)
    {
        var until = now + TimeSpan.FromSeconds(RoundSeconds);
        return until < maxEnd ? until : maxEnd;
    }

    public static bool StillGoing(TimeSpan now, TimeSpan expires, TimeSpan maxEnd)
    {
        return now < expires && now < maxEnd;
    }

    public static void HalvePhysical(DamageSpecifier damage)
    {
        Halve(damage, "Blunt");
        Halve(damage, "Piercing");
        Halve(damage, "Slash");
    }

    private static void Halve(DamageSpecifier damage, string id)
    {
        ProtoId<DamageTypePrototype> type = id;
        if (!damage.DamageDict.TryGetValue(type, out var value) || value.Float() <= 0)
            return;

        var amount = (int)Math.Floor(value.Float() / 2f);
        if (amount <= 0)
            damage.DamageDict.Remove(type);
        else
            damage.DamageDict[type] = FixedPoint2.New(amount);
    }
}
