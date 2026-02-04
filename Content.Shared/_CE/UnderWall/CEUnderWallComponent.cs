namespace Content.Shared._CE.UnderWall;

/// <summary>
///     Decreases the drawDepth of an entity by 1 if it is behind a wall.
///     Client-side only component, no network synchronization needed.
/// </summary>
[RegisterComponent]
public sealed partial class CEUnderWallComponent : Component
{
    /// <summary>
    ///     Original drawDepth value before modification
    /// </summary>
    [DataField]
    public int? OriginalDrawDepth;

    /// <summary>
    ///     Whether the entity is currently behind a wall
    /// </summary>
    [DataField]
    public bool IsBehindWall;
}
