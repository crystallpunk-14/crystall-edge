using Content.Shared._DND.Abilities;

namespace Content.Shared._DND.Classes;

public static class DndDivineOrders
{
    public const string Protector = "Protector";
    public const string Thaumaturge = "Thaumaturge";
}

public static class DndClericRules
{
    public const int ShieldOfFaithBonus = 2;

    public static int CantripCount(int baseCount, string? order)
    {
        if (order == DndDivineOrders.Thaumaturge)
            return baseCount + 1;

        return baseCount;
    }

    /// <summary>
    /// Added to Intelligence (Arcana) and Intelligence (Religion) checks. At least +1.
    /// </summary>
    public static int ThaumaturgeBonus(int wisdomScore)
    {
        return Math.Max(1, DndAbilityScores.Modifier(wisdomScore));
    }
}
