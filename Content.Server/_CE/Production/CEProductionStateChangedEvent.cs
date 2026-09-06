namespace Content.Server._CE.Production;

/// <summary>
/// Raised after the accumulator changes whether a produced output is pending.
/// Consumers decide which derived state must be refreshed.
/// </summary>
[ByRefEvent]
public readonly record struct CEProductionStateChangedEvent;
