using Content.Shared._CE.Trade.MainQuest;
using Robust.Shared.Prototypes;

namespace Content.Server._CE.GameTicking.Components;

/// <summary>
/// Config and progress for the Lucson Sphere crack/collapse cycle. The sphere entity itself
/// (<c>CEMurkLusconSphereComponent</c>) only holds its display <c>State</c> and base intensity;
/// all rules-of-the-round data lives here.
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
    /// How long the Restoration Ritual lasts before the sphere is restored.
    /// </summary>
    [DataField]
    public TimeSpan RitualDuration = TimeSpan.FromMinutes(2);

    /// <summary>
    /// When the Restoration Ritual started. Null until it does.
    /// </summary>
    [DataField]
    public TimeSpan? RitualStartTime;

    /// <summary>
    /// How fast the sphere's remaining intensity drains (units/sec) once it starts collapsing.
    /// </summary>
    [DataField]
    public float CollapseRate = 2f;

    /// <summary>
    /// Fraction of the sphere's base intensity left by the moment of collapse. After the crack the
    /// safe zone shrinks linearly from 1 to this value with the collapse progress.
    /// </summary>
    [DataField]
    public float ShrinkMultiplier = 0.7f;

    /// <summary>
    /// Lower bound of the random safe zone size (fraction of the base intensity) during the ritual.
    /// </summary>
    [DataField]
    public float RitualMinMultiplier = 0.4f;

    /// <summary>
    /// Upper bound of the random safe zone size (fraction of the base intensity) during the ritual.
    /// </summary>
    [DataField]
    public float RitualMaxMultiplier = 0.6f;

    /// <summary>
    /// Shortest time between two safe zone pulses during the ritual.
    /// </summary>
    [DataField]
    public TimeSpan PulseIntervalMin = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Longest time between two safe zone pulses during the ritual.
    /// </summary>
    [DataField]
    public TimeSpan PulseIntervalMax = TimeSpan.FromSeconds(3);

    /// <summary>
    /// When the safe zone pulses next during the ritual.
    /// </summary>
    [DataField]
    public TimeSpan NextPulseTime;

    /// <summary>
    /// While shrinking, the sphere's intensity is only rewritten (and networked) once the target
    /// drifts at least this far (in tiles) from the current value.
    /// </summary>
    [DataField]
    public float IntensityWriteThreshold = 0.25f;

    /// <summary>
    /// How often the round progress state is broadcast to clients.
    /// </summary>
    [DataField]
    public TimeSpan BroadcastInterval = TimeSpan.FromSeconds(1);

    [DataField]
    public TimeSpan NextBroadcast;

    /// <summary>
    /// How many lightheart shards the Restoration Ritual needs.
    /// </summary>
    [DataField]
    public int ShardCount = 4;

    /// <summary>
    /// How many prices the round rolls for the quest postaments: one per shard plus the restoration book.
    /// </summary>
    [DataField]
    public int PriceCount = 5;

    /// <summary>
    /// The round's prices, rolled when the rule starts. Price number N (1-based) is <c>Prices[N - 1]</c>.
    /// </summary>
    [DataField]
    public List<ProtoId<CEMainQuestPricePrototype>> Prices = new();
}
