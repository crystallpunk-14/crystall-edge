namespace Content.Server._CE.GameTicking;

/// <summary>
/// Broadcast whenever <see cref="CEMurkConsumingRuleSystem.AddMurk"/> raises the murk level.
/// </summary>
/// <param name="level">The new murk level, 0..1.</param>
public sealed class CEMurkLevelChangedEvent(float level) : EntityEventArgs
{
    public readonly float Level = level;
}
