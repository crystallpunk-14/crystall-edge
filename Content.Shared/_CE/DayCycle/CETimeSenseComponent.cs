using Robust.Shared.GameStates;

namespace Content.Shared._CE.DayCycle;

/// <summary>
/// Lets the player sense the time of day: shows a clock widget with the time left until dawn or sunset
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class CETimeSenseComponent : Component
{
}
