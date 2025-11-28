using Robust.Shared.GameStates;

namespace Content.Shared._CE.Actions.Components;

/// <summary>
/// Blocks the target from using magic if they are pacified.
/// Also block using spell on SSD player
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class CEActionDangerousComponent : Component
{
}
