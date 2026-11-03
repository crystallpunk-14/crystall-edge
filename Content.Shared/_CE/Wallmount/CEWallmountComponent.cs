namespace Content.Shared._CE.Wallmount;

/// <summary>
/// Attaches the entity to the wall behind it when it appears, or deletes it if there is none.
/// </summary>
[RegisterComponent, Access(typeof(CEWallmountSystem))]
public sealed partial class CEWallmountComponent : Component
{
    [DataField]
    public int AttachAttempts = 3;

    [DataField]
    public TimeSpan NextAttachTime = TimeSpan.Zero;

    /// <summary>
    /// The wall this entity hangs on, once attached.
    /// </summary>
    [DataField]
    public EntityUid? Wall;
}
