using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._CE.MurkShield;

/// <summary>
/// The single daily murk shield of the adventurers' guild. Follows the time of day of its z-network surface:
/// dims linearly from full strength at dawn down to <see cref="DuskFraction"/> at sunset, then collapses to nothing
/// over <see cref="CollapseDuration"/> and vanishes. Drives the murk source intensity and the point light radius together.
/// </summary>
[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class CEMurkShieldComponent : Component
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

    /// <summary>
    /// When the shield met its first night and started collapsing. Null while it lives through the day.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan? CollapseStart;
}
