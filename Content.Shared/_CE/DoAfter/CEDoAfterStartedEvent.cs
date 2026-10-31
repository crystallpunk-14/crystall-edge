namespace Content.Shared._CE.DoAfter;

/// <summary>
/// Raised directed on the user after a non-instant DoAfter has been started.
/// </summary>
[ByRefEvent]
public readonly record struct CEDoAfterStartedEvent(Shared.DoAfter.DoAfter DoAfter);
