using Robust.Shared.GameStates;

namespace Content.Shared._CE.Actions.Components;

/// <summary>
/// Lets the action be used while its performer is critical. Other actions that check consciousness are blocked.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class CEActionCastableFromCriticalComponent : Component;
