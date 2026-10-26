using Content.Shared._DND.Abilities;
using Robust.Shared.Prototypes;

namespace Content.Shared._DND.Backgrounds;

/// <summary>
/// Applies a background's ability increases. An increase cannot raise a score above 20.
/// </summary>
public static class DndBackgroundScores
{
    public const int IncreaseCap = 20;

    public static int Bonus(
        string abilityId,
        IReadOnlyList<ProtoId<DndAbilityPrototype>> listed,
        DndBackgroundIncrease increase,
        string plusTwo,
        string plusOne)
    {
        var isListed = false;
        foreach (var ability in listed)
        {
            if (ability.Id == abilityId)
                isListed = true;
        }

        if (!isListed)
            return 0;

        if (increase == DndBackgroundIncrease.AllThree)
            return 1;

        if (abilityId == plusTwo)
            return 2;

        if (abilityId == plusOne)
            return 1;

        return 0;
    }

    public static int FinalScore(int baseScore, int bonus)
    {
        if (bonus <= 0)
            return baseScore;

        return Math.Min(IncreaseCap, baseScore + bonus);
    }

    public static bool IsValidSplit(
        IReadOnlyList<ProtoId<DndAbilityPrototype>> listed,
        string plusTwo,
        string plusOne)
    {
        if (plusTwo == plusOne || listed.Count < 2)
            return false;

        var hasTwo = false;
        var hasOne = false;
        foreach (var ability in listed)
        {
            if (ability.Id == plusTwo)
                hasTwo = true;
            if (ability.Id == plusOne)
                hasOne = true;
        }

        return hasTwo && hasOne;
    }
}
