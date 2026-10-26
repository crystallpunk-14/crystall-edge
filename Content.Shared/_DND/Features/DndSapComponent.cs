namespace Content.Shared._DND.Features;

/// <summary>
/// The next attack this creature makes, before the time passes, is at disadvantage.
/// </summary>
[RegisterComponent]
public sealed partial class DndSapComponent : Component
{
    [DataField]
    public TimeSpan Until;
}
