using Robust.Shared.GameStates;

namespace Content.Shared._CE.ZLevels;

/// <summary>
/// Allows entities not to fall if they are above this entity at a higher level.
/// Think of it as the ability to walk on top of walls, for example.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CEZLevelSupportComponent : Component
{
    /// <summary>
    /// Support height. Keep values between 0 and 1. If the value = 1, the height will be ideal so that you can essentially walk on the tile from above.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Height = 1f;

    /// <summary>
    /// The tile where this entity is attached is sloped in terms of Z-levels.
    /// You can smoothly climb up to the upper level or descend downwards along it.
    /// </summary>
    [DataField]
    public bool Slope = false;
}
