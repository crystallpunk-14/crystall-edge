using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server._CE.Weather.Components;

/// <summary>
/// When added to a weather status effect entity (alongside <see cref="Content.Shared.Weather.WeatherStatusEffectComponent"/>),
/// enables periodic gameplay effect cycles handled by <see cref="CEWeatherEffectsSystem"/>.
/// </summary>
[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class CEWeatherEffectsComponent : Component
{
    /// <summary>
    /// The minimum interval between effect application cycles.
    /// </summary>
    [DataField]
    public TimeSpan MinEffectFrequency = TimeSpan.FromSeconds(1f);

    /// <summary>
    /// The maximum interval between effect application cycles.
    /// </summary>
    [DataField]
    public TimeSpan MaxEffectFrequency = TimeSpan.FromSeconds(5f);

    /// <summary>
    /// The time at which the next effect cycle should trigger.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextEffectTime;
}
