using Content.Shared.Chemistry.Reagent;

namespace Content.Server._CE.Weather.Components;

/// <summary>
/// When added to a weather status effect entity (alongside <see cref="Content.Shared.Weather.WeatherStatusEffectComponent"/>),
/// periodically pours the listed reagents into every open, sky-exposed <see cref="CEWeatherSolutionRefillableComponent"/>.
/// Handled by <see cref="CEWeatherSolutionFillSystem"/>.
/// </summary>
[RegisterComponent]
public sealed partial class CEWeatherSolutionFillComponent : Component
{
    /// <summary>
    /// Reagents (and how much of each) poured in per fill cycle.
    /// </summary>
    [DataField(required: true)]
    public List<ReagentQuantity> Reagents = new();

    /// <summary>
    /// How often a fill cycle happens for a given container.
    /// </summary>
    [DataField]
    public TimeSpan Frequency = TimeSpan.FromSeconds(10);
}
