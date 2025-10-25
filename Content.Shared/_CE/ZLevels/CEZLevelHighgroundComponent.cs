using Robust.Shared.GameStates;

namespace Content.Shared._CE.ZLevels;

/// <summary>
/// Allows entities not to fall if they are above this entity at a higher level.
/// Think of it as the ability to walk on top of walls, for example.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CEZLevelHighgroundComponent : Component
{
    /// <summary>
    /// Height profile points, forming a simple curve (0..1 by X, height by Y).
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<float> HeightCurve = new()
    {
        1f,
        1f,
    };
}
