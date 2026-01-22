using Robust.Shared.GameStates;

namespace Content.Shared._CE.ZLevels.Pulling;

/// <summary>
/// Specifies an entity as being pullable by an entity with <see cref="PullerComponent"/>
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class CEZLevelPullableComponent : Component
{
}

