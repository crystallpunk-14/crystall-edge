using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server._CE.Weather.Components;

/// <summary>
/// When added to a weather status effect entity (alongside <see cref="Content.Shared.Weather.WeatherStatusEffectComponent"/>),
/// enables periodic "strike a random exposed tile" cycles handled by <see cref="CEWeatherTileEffectsSystem"/>.
/// Unlike <see cref="CEWeatherEffectsComponent"/>, this targets a random open-sky coordinate rather than
/// scanning for entities — e.g. for lightning strikes.
/// </summary>
[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class CEWeatherTileEffectsComponent : Component
{
    /// <summary>
    /// The minimum interval between tile strike cycles.
    /// </summary>
    [DataField]
    public TimeSpan MinEffectFrequency = TimeSpan.FromSeconds(5f);

    /// <summary>
    /// The maximum interval between tile strike cycles.
    /// </summary>
    [DataField]
    public TimeSpan MaxEffectFrequency = TimeSpan.FromSeconds(15f);

    /// <summary>
    /// The time at which the next tile strike cycle should trigger.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextEffectTime;
}
