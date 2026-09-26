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
    /// Days (inclusive) the sphere can hold out after cracking before it collapses.
    /// </summary>
    [DataField]
    public int DaysToCollapse = 7;

    /// <summary>
    /// Days passed since the sphere cracked.
    /// </summary>
    [DataField]
    public int DaysSinceCrack;

    /// <summary>
    /// How much the sphere's dispel intensity weakens (moves toward 0) each day after cracking.
    /// </summary>
    [DataField]
    public float IntensityPerDay = 1f;

    /// <summary>
    /// How fast the sphere's remaining intensity drains (units/sec) once it starts collapsing.
    /// </summary>
    [DataField]
    public float CollapseRate = 2f;
}
