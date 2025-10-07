using Content.Shared.Actions;

namespace Content.Shared._CE.Roof;

/// <summary>
/// Marks an entity as a roof, allowing you to hide all roofs or show them back depending on different situations
/// </summary>
[RegisterComponent]
public sealed partial class CERoofComponent : Component
{
    /// <summary>
    /// The original alpha value of the sprite before any transitions
    /// </summary>
    public float OriginalAlpha = 1.0f;

    /// <summary>
    /// Whether the roof is currently in a transitioning state
    /// </summary>
    public bool IsTransitioning = false;
}

public sealed partial class CEToggleRoofVisibilityAction : InstantActionEvent
{
}
