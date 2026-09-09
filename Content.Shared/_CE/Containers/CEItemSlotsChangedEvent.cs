namespace Content.Shared._CE.Containers;

/// <summary>
/// The native slot owner started, finished shutdown ejection, or changed a slot lock.
/// Consumers read the owner's current component lifecycle and slot state.
/// Occupant changes continue to use the native container messages.
/// </summary>
[ByRefEvent]
public readonly struct CEItemSlotsChangedEvent;
