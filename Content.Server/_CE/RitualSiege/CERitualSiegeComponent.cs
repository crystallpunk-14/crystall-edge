using Content.Shared.EntityTable.EntitySelectors;
using Robust.Shared.Prototypes;

namespace Content.Server._CE.RitualSiege;

/// <summary>
/// Sits next to <c>CEMurkConsumingRuleComponent</c> on the round rule. Once the Restoration Ritual starts,
/// alternates calm and danger phases: each danger phase spawns a wave of murk mobs around the Lucson Sphere,
/// and each phase sets its own storm on the sphere's z-network instead of the station weather.
/// </summary>
[RegisterComponent, Access(typeof(CERitualSiegeSystem))]
public sealed partial class CERitualSiegeComponent : Component
{
    /// <summary>
    /// Rolled once per wave roll: each roll spawns whatever the table yields.
    /// </summary>
    [DataField(required: true)]
    public EntityTableSelector Table = default!;

    /// <summary>
    /// Wave rolls regardless of the online.
    /// </summary>
    [DataField]
    public float BaseRolls = 1f;

    /// <summary>
    /// Wave rolls added for the round's peak online: <c>RollsPerPlayer × online ^ PlayerExponent</c>.
    /// </summary>
    [DataField]
    public float RollsPerPlayer = 0.8f;

    /// <summary>
    /// Below 1 every next player adds less threat: a crowd fights worse because of friendly fire and the crush.
    /// </summary>
    [DataField]
    public float PlayerExponent = 0.5f;

    /// <summary>
    /// Wave rolls multiplier at the start of the ritual, lerped to <see cref="EndMultiplier"/> by its end.
    /// </summary>
    [DataField]
    public float StartMultiplier = 1f;

    [DataField]
    public float EndMultiplier = 2f;

    /// <summary>
    /// Mobs spawn on free tiles between this and <see cref="MaxSpawnRadius"/> tiles from the sphere, on its z-level.
    /// </summary>
    [DataField]
    public float MinSpawnRadius = 3f;

    [DataField]
    public float MaxSpawnRadius = 7f;

    /// <summary>
    /// Random tiles tried per mob before it is skipped.
    /// </summary>
    [DataField]
    public int SpawnAttempts = 20;

    /// <summary>
    /// How long spawned mobs remember where the sphere is.
    /// </summary>
    [DataField]
    public TimeSpan SphereMemory = TimeSpan.FromHours(1);

    /// <summary>
    /// The calm the ritual opens with, so the defenders can take their positions.
    /// </summary>
    [DataField]
    public TimeSpan FirstCalmDuration = TimeSpan.FromSeconds(10);

    // With the ~125 s ritual track these phases fit at least 4 waves (usually 5).
    [DataField]
    public TimeSpan MinCalmDuration = TimeSpan.FromSeconds(8);

    [DataField]
    public TimeSpan MaxCalmDuration = TimeSpan.FromSeconds(12);

    [DataField]
    public TimeSpan MinDangerDuration = TimeSpan.FromSeconds(15);

    [DataField]
    public TimeSpan MaxDangerDuration = TimeSpan.FromSeconds(20);

    [DataField]
    public EntProtoId CalmWeather = "CEWeatherMurkStorm";

    [DataField]
    public EntProtoId DangerWeather = "CEWeatherMurkStormStrong";

    /// <summary>
    /// The sphere under siege. Null until the ritual starts.
    /// </summary>
    [DataField]
    public EntityUid? Sphere;

    /// <summary>
    /// The z-network the siege weather is set on. Null if the sphere's map belongs to none.
    /// </summary>
    [DataField]
    public EntityUid? Network;

    [DataField]
    public bool Danger;

    [DataField]
    public TimeSpan NextPhaseTime;
}
