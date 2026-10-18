using Robust.Shared.GameStates;

namespace Content.Shared._CE.GOAP.Components;

/// <summary>
/// Marks a sleeping GOAP agent: it can't wake through normal checks until <c>CEGOAPSleepingSystem</c> removes this.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class CEGOAPSleepingComponent : Component;
