using Robust.Shared.GameStates;

namespace Content.Shared._CE.EntitySlots;

/// <summary>
/// Opts a fixed-slot host into direct interaction with its visible occupants.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class CEFixedEntitySlotAccessComponent : Component;

/// <summary>
/// Runtime marker replicated to an occupant while it belongs to an accessible fixed slot.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class CEFixedEntitySlotAccessibleOccupantComponent : Component;
