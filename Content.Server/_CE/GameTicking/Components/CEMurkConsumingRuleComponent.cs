namespace Content.Server._CE.GameTicking.Components;

/// <summary>
/// Config and progress for the Lucson Sphere crack/collapse cycle. The sphere entity itself
/// (<c>CEMurkLusconSphereComponent</c>) only holds its display <c>State</c>; all rules-of-the-round
/// data lives here.
/// </summary>
[RegisterComponent, Access(typeof(CEMurkConsumingRuleSystem))]
public sealed partial class CEMurkConsumingRuleComponent : Component
{
    /// <summary>
    /// Time after round start until the Lucson Sphere cracks and secret role goals are revealed.
    /// </summary>
    [DataField]
    public TimeSpan CrackDelay = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Real time after the sphere cracks until it collapses.
    /// </summary>
    [DataField]
    public TimeSpan CollapseDelay = TimeSpan.FromMinutes(60);

    /// <summary>
    /// When the sphere cracked. Null until it does.
    /// </summary>
    [DataField]
    public TimeSpan? CrackTime;

    /// <summary>
    /// When the sphere left the cracked state (fixed or collapsed). Freezes the collapse progress.
    /// </summary>
    [DataField]
    public TimeSpan? CrackEndTime;

    /// <summary>
    /// How fast the sphere's remaining intensity drains (units/sec) once it starts collapsing.
    /// </summary>
    [DataField]
    public float CollapseRate = 2f;

    /// <summary>
    /// How often the round progress state is broadcast to clients.
    /// </summary>
    [DataField]
    public TimeSpan BroadcastInterval = TimeSpan.FromSeconds(1);

    [DataField]
    public TimeSpan NextBroadcast;
}
