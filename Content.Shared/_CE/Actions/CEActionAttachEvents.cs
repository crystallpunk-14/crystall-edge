namespace Content.Shared._CE.Actions;

/// <summary>
/// Raised directed at an action entity right after it gets attached to a performer - on first grant,
/// and again whenever it is re-granted (e.g. a mind's actions moving to a new body).
/// </summary>
[ByRefEvent]
public readonly record struct CEActionGrantedEvent(EntityUid Performer);

/// <summary>
/// Raised directed at an action entity right after it gets detached from its performer.
/// </summary>
[ByRefEvent]
public readonly record struct CEActionRevokedEvent(EntityUid Performer);
