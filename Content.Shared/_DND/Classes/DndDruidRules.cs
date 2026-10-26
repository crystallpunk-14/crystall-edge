using Content.Shared._DND.Abilities;

namespace Content.Shared._DND.Classes;

public static class DndPrimalOrders
{
    public const string Magician = "Magician";
    public const string Warden = "Warden";
}

public static class DndDruidRules
{
    public const int SignDifficultyClass = 15;

    public static int CantripCount(int baseCount, string? order)
    {
        if (order == DndPrimalOrders.Magician)
            return baseCount + 1;

        return baseCount;
    }

    /// <summary>
    /// Added to Intelligence (Arcana) and Intelligence (Nature) checks. At least +1.
    /// </summary>
    public static int MagicianBonus(int wisdomScore)
    {
        return Math.Max(1, DndAbilityScores.Modifier(wisdomScore));
    }
}
