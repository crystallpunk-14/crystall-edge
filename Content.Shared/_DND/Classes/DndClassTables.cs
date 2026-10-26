namespace Content.Shared._DND.Classes;

/// <summary>
/// Reads a per-level column. Index 0 is level 1. A short column keeps its last value.
/// </summary>
public static class DndClassTables
{
    public static int AtLevel(IReadOnlyList<int> table, int level, int fallback)
    {
        if (table.Count == 0)
            return fallback;

        var index = Math.Clamp(level, 1, table.Count) - 1;
        return table[index];
    }
}
