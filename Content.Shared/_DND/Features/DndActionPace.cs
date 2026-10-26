namespace Content.Shared._DND.Features;

/// <summary>
/// How long a character waits before the same action can be used again.
/// An action is 6 seconds. A bonus action is 3 seconds.
/// </summary>
public static class DndActionPace
{
    public const float ActionSeconds = 6f;
    public const float BonusActionSeconds = 3f;
}
