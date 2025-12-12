namespace Content.Shared._CE.LockKey.Components;

/// <summary>
/// Randomly generates the key shape during initialization. Useful for any random dungeons that do not have keys.
/// </summary>
[RegisterComponent]
public sealed partial class CELockRandomShapeComponent : Component
{
    /// <summary>
    /// Length of the shape to be generated.
    /// </summary>
    [DataField(required: true)]
    public int Length;
}
