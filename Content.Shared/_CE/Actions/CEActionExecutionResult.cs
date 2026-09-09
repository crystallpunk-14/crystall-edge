namespace Content.Shared._CE.Actions;

/// <summary>The outcome of the normal action request pipeline, including asynchronous acceptance.</summary>
public enum CEActionExecutionResult : byte
{
    Unavailable,
    InvalidTarget,
    Started,
    Performed,
    Unhandled,
}
