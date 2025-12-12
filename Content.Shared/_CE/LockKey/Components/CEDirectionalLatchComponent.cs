using Robust.Shared.GameStates;

namespace Content.Shared._CE.LockKey.Components;

/// <summary>
/// Allows you to lock or unlock locks while standing on a specific side of the object. Used for window bolts that can only be opened from inside the building.
/// </summary>
[RegisterComponent, NetworkedComponent,
 Access(typeof(CESharedLockKeySystem))]
public sealed partial class CEDirectionalLatchComponent : Component
{
    /// <summary>
    /// Which side should you stand on to open this lock?
    /// </summary>
    [DataField]
    public Direction Direction = Direction.South;
}
