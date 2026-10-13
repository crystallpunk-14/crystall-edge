using Robust.Shared.GameStates;

namespace Content.Shared._CE.DayCycle;

/// <summary>
/// Status effect that lets the affected entity sense the time of day: shows a clock widget with the time left until dawn or sunset
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class CETimeSenseStatusEffectComponent : Component
{
}
