namespace Content.Server._CE.Workbench;

/// <summary>
/// Raised on an autocrafter when it starts crafting its selected recipe.
/// </summary>
[ByRefEvent]
public readonly record struct CEAutoCraftStartedEvent(TimeSpan Duration);

/// <summary>
/// Raised on an autocrafter when a craft completes successfully.
/// </summary>
[ByRefEvent]
public readonly record struct CEAutoCraftFinishedEvent;

/// <summary>
/// Raised on an autocrafter when a craft is interrupted or fails at the last moment.
/// </summary>
[ByRefEvent]
public readonly record struct CEAutoCraftCancelledEvent;
