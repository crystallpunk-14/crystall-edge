namespace Content.Server._CE.GameTicking.Components;

[RegisterComponent, Access(typeof(CEMurkConsumingRuleSystem))]
public sealed partial class CEMurkConsumingRuleComponent : Component
{
    /// <summary>
    /// Time after round start until the Lucson Sphere cracks and secret role goals are revealed.
    /// </summary>
    [DataField]
    public TimeSpan CrackDelay = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Days (inclusive) the sphere can hold out after cracking before it collapses.
    /// </summary>
    [DataField]
    public int Max = 7;

    /// <summary>
    /// Days passed since the sphere cracked.
    /// </summary>
    [DataField]
    public int Current;
}
