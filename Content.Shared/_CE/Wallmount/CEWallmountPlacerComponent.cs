using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Wallmount;

/// <summary>
/// Item that mounts a wallmount entity on the side of a clicked wall facing the user, consuming one item from the stack.
/// </summary>
[RegisterComponent, Access(typeof(CEWallmountPlacerSystem))]
public sealed partial class CEWallmountPlacerComponent : Component
{
    [DataField(required: true)]
    public EntProtoId Prototype;

    [DataField]
    public TimeSpan Delay = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Also allows clicking an open tile next to the user: the entity is mounted one level below, on the wall
    /// the user stands on, facing the clicked tile. Used by ladders leading down.
    /// </summary>
    [DataField]
    public bool AllowPlaceBelow;
}
