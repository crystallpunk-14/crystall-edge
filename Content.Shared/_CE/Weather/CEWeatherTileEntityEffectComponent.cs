using Content.Shared._CE.EntityEffect;
using Robust.Shared.GameStates;

namespace Content.Shared._CE.Weather;

/// <summary>
/// When added to a weather status effect entity (alongside <see cref="Content.Shared.Weather.WeatherStatusEffectComponent"/>),
/// defines <see cref="CEEntityEffect"/>s that are applied at a randomly struck open-sky tile
/// (no target entity — e.g. a <c>SpawnEntity</c> effect for a lightning strike).
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class CEWeatherTileEntityEffectComponent : Component
{
    /// <summary>
    /// The CE entity effects to apply at the struck tile.
    /// </summary>
    [DataField(required: true)]
    public List<CEEntityEffect> Effects = new();

    /// <summary>
    /// Power multiplier passed to the effects (e.g. weather strength).
    /// </summary>
    [DataField]
    public float Power = 1f;
}
