using Robust.Shared.GameStates;

namespace Content.Shared._CE.ZLevels;

/// <summary>
/// Allows entities not to fall if they are above this entity at a higher level.
/// Think of it as the ability to walk on top of walls, for example.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class CEZLevelSupportComponent : Component
{
}
