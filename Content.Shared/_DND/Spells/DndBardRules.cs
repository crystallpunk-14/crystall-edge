using Content.Shared._DND.Abilities;

namespace Content.Shared._DND.Spells;

public static class DndBardRules
{
    public static int InspirationUses(int charismaScore)
    {
        return Math.Max(1, DndAbilityScores.Modifier(charismaScore));
    }

    public static int SlotsAfterCast(int spellLevel, int slots)
    {
        if (spellLevel <= 0)
            return slots;

        return Math.Max(0, slots - 1);
    }
}
