namespace Content.Shared._DND.Abilities;

/// <summary>
/// Ability-score math. Division rounds down, including when the fraction is one half or greater.
/// </summary>
public static class DndAbilityScores
{
    public const int AbsoluteMinimum = 1;
    public const int AbsoluteMaximum = 30;
    public const int StartingLevel = 1;
    public const int MaxLevel = 20;
    public const int BaseArmorClassValue = 10;
    public const int PointBuyBudget = 27;
    public const int PointBuyMinimum = 8;
    public const int PointBuyMaximum = 15;

    private static readonly int[] StandardArrayScores = { 15, 14, 13, 12, 10, 8 };

    /// <summary>
    /// Modifier applied to a d20 test made with this ability. Equals floor((score - 10) / 2).
    /// </summary>
    public static int Modifier(int score)
    {
        var delta = score - 10;
        if (delta >= 0)
            return delta / 2;

        // Integer division truncates toward zero. Subtracting 1 first rounds negative halves down.
        return (delta - 1) / 2;
    }

    /// <summary>
    /// Bonus from the character advancement table. +2 at level 1, then +1 at each fourth level after that.
    /// </summary>
    public static int ProficiencyBonus(int level)
    {
        var clamped = Math.Clamp(level, StartingLevel, MaxLevel);
        return 2 + (clamped - 1) / 4;
    }

    /// <summary>
    /// The one base Armor Class every creature starts with: 10 + Dexterity modifier.
    /// A class feature or spell may offer a different base later. Only one base calculation is in effect.
    /// </summary>
    public static int BaseArmorClass(int dexterityScore)
    {
        return BaseArmorClassValue + Modifier(dexterityScore);
    }

    public static int[] CopyStandardArray()
    {
        return (int[])StandardArrayScores.Clone();
    }

    /// <summary>
    /// Point-buy cost for a score from 8 to 15. 14 costs 7 and 15 costs 9.
    /// </summary>
    public static bool TryPointBuyCost(int score, out int cost)
    {
        switch (score)
        {
            case 8:
                cost = 0;
                return true;
            case 9:
                cost = 1;
                return true;
            case 10:
                cost = 2;
                return true;
            case 11:
                cost = 3;
                return true;
            case 12:
                cost = 4;
                return true;
            case 13:
                cost = 5;
                return true;
            case 14:
                cost = 7;
                return true;
            case 15:
                cost = 9;
                return true;
            default:
                cost = 0;
                return false;
        }
    }

    public static int PointBuySpent(IReadOnlyList<string> abilityIds, IReadOnlyDictionary<string, int> scores)
    {
        var spent = 0;
        foreach (var abilityId in abilityIds)
        {
            var score = scores.TryGetValue(abilityId, out var found) ? found : PointBuyMinimum;
            if (!TryPointBuyCost(score, out var cost))
                return PointBuyBudget + 1;

            spent += cost;
        }

        return spent;
    }

    public static bool IsStandardArray(IReadOnlyList<int> scores)
    {
        if (scores.Count != StandardArrayScores.Length)
            return false;

        var actual = new int[scores.Count];
        for (var i = 0; i < scores.Count; i++)
            actual[i] = scores[i];

        Array.Sort(actual);
        for (var i = 0; i < actual.Length; i++)
        {
            if (actual[i] != StandardArrayScores[StandardArrayScores.Length - 1 - i])
                return false;
        }

        return true;
    }

    /// <summary>
    /// Keeps a valid assignment. An invalid set is rewritten by putting the highest current scores on the highest array values.
    /// </summary>
    public static Dictionary<string, int> NormalizeStandardArray(
        IReadOnlyList<string> abilityIds,
        IReadOnlyDictionary<string, int> current)
    {
        var values = new List<int>(abilityIds.Count);
        foreach (var abilityId in abilityIds)
            values.Add(current.TryGetValue(abilityId, out var score) ? score : 0);

        if (IsStandardArray(values))
        {
            var kept = new Dictionary<string, int>(abilityIds.Count);
            for (var i = 0; i < abilityIds.Count; i++)
                kept[abilityIds[i]] = values[i];

            return kept;
        }

        var ranked = new List<(string Id, int Score, int Index)>(abilityIds.Count);
        for (var i = 0; i < abilityIds.Count; i++)
        {
            var abilityId = abilityIds[i];
            var score = current.TryGetValue(abilityId, out var found) ? found : 0;
            ranked.Add((abilityId, score, i));
        }

        ranked.Sort(static (a, b) =>
        {
            var byScore = b.Score.CompareTo(a.Score);
            return byScore != 0 ? byScore : a.Index.CompareTo(b.Index);
        });

        var assigned = new Dictionary<string, int>(abilityIds.Count);
        var array = CopyStandardArray();
        for (var i = 0; i < ranked.Count; i++)
            assigned[ranked[i].Id] = i < array.Length ? array[i] : PointBuyMinimum;

        return assigned;
    }

    /// <summary>
    /// Clamps each score to 8–15 and lowers the highest scores until the total cost is within 27 points.
    /// </summary>
    public static Dictionary<string, int> NormalizePointBuy(
        IReadOnlyList<string> abilityIds,
        IReadOnlyDictionary<string, int> current)
    {
        var scores = new Dictionary<string, int>(abilityIds.Count);
        foreach (var abilityId in abilityIds)
        {
            var score = current.TryGetValue(abilityId, out var found) ? found : PointBuyMinimum;
            score = Math.Clamp(score, PointBuyMinimum, PointBuyMaximum);
            if (!TryPointBuyCost(score, out _))
                score = PointBuyMinimum;

            scores[abilityId] = score;
        }

        while (PointBuySpent(abilityIds, scores) > PointBuyBudget)
        {
            string? highestId = null;
            var highestScore = PointBuyMinimum;
            foreach (var abilityId in abilityIds)
            {
                var score = scores[abilityId];
                if (score <= highestScore)
                    continue;

                highestScore = score;
                highestId = abilityId;
            }

            if (highestId == null)
                break;

            scores[highestId] = highestScore - 1;
        }

        return scores;
    }

    public static List<string> OrderedIds(IEnumerable<DndAbilityPrototype> abilities)
    {
        var list = new List<DndAbilityPrototype>();
        foreach (var ability in abilities)
            list.Add(ability);

        list.Sort(static (a, b) =>
        {
            var order = a.Order.CompareTo(b.Order);
            return order != 0 ? order : string.CompareOrdinal(a.ID, b.ID);
        });

        var ids = new List<string>(list.Count);
        foreach (var ability in list)
            ids.Add(ability.ID);

        return ids;
    }

    public static string FormatSigned(int value)
    {
        if (value > 0)
            return $"+{value}";

        if (value == 0)
            return "+0";

        return value.ToString();
    }

    /// <summary>
    /// Localization id for what a score of this magnitude means.
    /// </summary>
    public static LocId Band(int score)
    {
        return score switch
        {
            <= 1 => "dnd-ability-band-lowest",
            <= 9 => "dnd-ability-band-weak",
            <= 11 => "dnd-ability-band-average",
            <= 19 => "dnd-ability-band-strong",
            20 => "dnd-ability-band-cap",
            <= 29 => "dnd-ability-band-extraordinary",
            _ => "dnd-ability-band-maximum",
        };
    }
}
