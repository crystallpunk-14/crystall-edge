using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._CE.GuildLight;

/// <summary>
/// The single daily light of the adventurers' guild. Dims linearly from full strength at dawn down to
/// <see cref="DuskFraction"/> at sunset, then collapses to nothing over <see cref="CollapseDuration"/> and vanishes.
/// Drives the murk source intensity and the point light radius together.
/// </summary>
[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class CEGuildLightComponent : Component
{
    /// <summary>
    /// Murk source intensity at full charge, negative dispels murk.
    /// </summary>
    [DataField]
    public float MaxIntensity = -7f;

    /// <summary>
    /// Share of <see cref="MaxIntensity"/> left by sunset.
    /// </summary>
    [DataField]
    public float DuskFraction = 0.5f;

    [DataField]
    public TimeSpan CollapseDuration = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Smallest intensity change worth dirtying the entity for.
    /// </summary>
    [DataField]
    public float SyncThreshold = 0.05f;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan DawnTime;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan SunsetTime;
}
