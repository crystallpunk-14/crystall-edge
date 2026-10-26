namespace Content.Shared._DND.Backgrounds;

/// <summary>
/// How the background's three ability increases are split.
/// </summary>
public enum DndBackgroundIncrease
{
    /// <summary>
    /// Each of the three abilities increases by 1.
    /// </summary>
    AllThree = 0,

    /// <summary>
    /// One listed ability increases by 2 and a different listed ability increases by 1.
    /// </summary>
    Split = 1,
}
