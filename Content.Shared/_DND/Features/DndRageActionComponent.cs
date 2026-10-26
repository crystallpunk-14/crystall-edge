namespace Content.Shared._DND.Features;

/// <summary>
/// Marks a rage action so it can still be pressed with no uses left, which only refreshes a rage that is already up.
/// </summary>
[RegisterComponent]
public sealed partial class DndRageActionComponent : Component;
