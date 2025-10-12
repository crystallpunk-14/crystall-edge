namespace Content.Shared._CE.Wallmount;

/// <summary>
/// Automatically attaches the entity to the wall when it appears, or removes it
/// </summary>
[RegisterComponent, Access(typeof(CEWallmountSystem))]
public sealed partial class CEWallmountComponent : Component
{
    [DataField]
    public int AttachAttempts = 3;

    [DataField]
    public TimeSpan NextAttachTime = TimeSpan.Zero;
}
