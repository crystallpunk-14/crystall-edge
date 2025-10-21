namespace Content.Server._CE.ZLevels.Components;

/// <summary>
/// Tracks all Z-level eyes located on other maps
/// </summary>
[RegisterComponent, UnsavedComponent]
public sealed partial class CEZLevelViewerComponent : Component
{
    public HashSet<EntityUid> Eyes = new();
}
