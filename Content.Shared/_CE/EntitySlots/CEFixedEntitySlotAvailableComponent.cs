using Robust.Shared.GameStates;

namespace Content.Shared._CE.EntitySlots;

/// <summary>
/// Runtime capability marker applied while a fixed-slot host can accept another occupant.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class CEFixedEntitySlotsAvailableComponent : Component;
