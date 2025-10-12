namespace Content.Shared._CE.Wallmount;

/// <summary>
/// Stores a list of all entities that are “attached” to this object. Destroying this object will destroy all attached entities
/// </summary>
[RegisterComponent, Access(typeof(CEWallmountSystem))]
public sealed partial class CEWallmountedComponent : Component
{
    [DataField]
    public HashSet<EntityUid> Attached = new();
}
