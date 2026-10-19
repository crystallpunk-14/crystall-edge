namespace Content.Shared._CE.EntityEffect.Effects;

/// <summary>
/// Starts a conversation between the user and the target. The user's opening line counts as the first one,
/// so it should be said by the same effect list.
/// </summary>
public sealed partial class StartMurkConversation : CEEntityEffectBase<StartMurkConversation>
{
    /// <summary>
    /// Total lines said by both sides, the opening one included.
    /// </summary>
    [DataField]
    public int Lines = 4;

    [DataField]
    public TimeSpan MinTurnDelay = TimeSpan.FromSeconds(1.2);

    [DataField]
    public TimeSpan MaxTurnDelay = TimeSpan.FromSeconds(4);

    /// <summary>
    /// How long a speaker may hold the turn before the conversation is dropped.
    /// </summary>
    [DataField]
    public TimeSpan TurnTimeout = TimeSpan.FromSeconds(8);

    /// <summary>
    /// The conversation is dropped once the sides are farther apart than this.
    /// </summary>
    [DataField]
    public float MaxDistance = 4f;
}

/// <summary>
/// Turns the user to its conversation partner and hands the turn over after its line.
/// </summary>
public sealed partial class PassMurkConversationTurn : CEEntityEffectBase<PassMurkConversationTurn>
{
    public PassMurkConversationTurn()
    {
        EffectTarget = CEEffectTarget.User;
    }
}
