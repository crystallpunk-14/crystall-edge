namespace Content.Server._CE.Murk.Components;

/// <summary>
/// Marks a participant of an ongoing conversation. Both sides carry it.
/// </summary>
[RegisterComponent, Access(typeof(CEMurkConversationSystem))]
public sealed partial class CEMurkConversationComponent : Component
{
    [ViewVariables]
    public EntityUid Partner;

    /// <summary>
    /// Lines left until the conversation ends, shared by both sides.
    /// </summary>
    [ViewVariables]
    public int LinesLeft;

    [ViewVariables]
    public TimeSpan MinTurnDelay;

    [ViewVariables]
    public TimeSpan MaxTurnDelay;

    /// <summary>
    /// How long a speaker may hold the turn before the conversation is dropped.
    /// </summary>
    [ViewVariables]
    public TimeSpan TurnTimeout;

    [ViewVariables]
    public float MaxDistance;

    /// <summary>
    /// When this side gets the turn. Null while the turn is not scheduled for it.
    /// </summary>
    [ViewVariables]
    public TimeSpan? TurnAt;

    /// <summary>
    /// The conversation is dropped once this time passes.
    /// </summary>
    [ViewVariables]
    public TimeSpan Deadline;
}
