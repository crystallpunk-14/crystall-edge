using Content.Shared._DND.Abilities;

namespace Content.Shared._DND.Features;

/// <summary>
/// d20 checks and saves. Advantage and disadvantage cancel. The kept die is the one that can be a natural 20 or a natural 1.
/// </summary>
public static class DndChecks
{
    public static int Bonus(int score, bool proficient, int proficiencyBonus)
    {
        var bonus = DndAbilityScores.Modifier(score);
        if (proficient)
            bonus += proficiencyBonus;

        return bonus;
    }

    public static int DifficultyClass(int abilityScore, int proficiencyBonus)
    {
        return 8 + DndAbilityScores.Modifier(abilityScore) + proficiencyBonus;
    }

    public static int KeptRoll(int first, int second, bool advantage, bool disadvantage)
    {
        if (advantage == disadvantage)
            return first;

        return advantage ? Math.Max(first, second) : Math.Min(first, second);
    }

    public static bool Succeeds(int naturalRoll, int bonus, int difficultyClass)
    {
        return naturalRoll + bonus >= difficultyClass;
    }
}
