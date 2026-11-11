using Robust.Shared.GameStates;

namespace Content.Shared._CE.DayCycle;

/// <summary>
/// Shows the clock widget with the time left until dawn or sunset.
/// Works on worn clothing and on status effects, never put it on the player directly.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class CETimeSenseComponent : Component
{
}
