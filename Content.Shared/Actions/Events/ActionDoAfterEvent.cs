using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Actions.Events;

/// <summary>
/// The event that triggers when an action doafter is completed or cancelled
/// </summary>
[Serializable, NetSerializable]
public sealed partial class ActionDoAfterEvent : DoAfterEvent
{
    /// <summary>
    /// The action performer
    /// </summary>
    public readonly NetEntity Performer;

    /// <summary>
    /// The original action use delay, used for repeating actions
    /// </summary>
    public readonly TimeSpan? OriginalUseDelay;

    /// <summary>
    /// The original request, for validating
    /// </summary>
    public readonly RequestPerformActionEvent Input;

    // CrystallEdge: keep presentation choices with the request during delayed/repeated execution.
    public readonly bool Predicted;
    public readonly bool ShowPopups;

    public ActionDoAfterEvent(NetEntity performer, TimeSpan? originalUseDelay, RequestPerformActionEvent input, bool predicted = true, bool showPopups = true)
    {
        Performer = performer;
        OriginalUseDelay = originalUseDelay;
        Input = input;
        Predicted = predicted;
        ShowPopups = showPopups;
    }
    // CrystallEdge end

    public override DoAfterEvent Clone() => this;
}
