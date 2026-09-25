using System.Numerics;
using Content.Shared.StatusEffectNew.Components;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared.Weather;

/// <summary>
/// Used only in conjure with <see cref="StatusEffectComponent"/> for status effects applied to map entities.
/// Contains basic information about all types of weather effects.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedWeatherSystem))]
public sealed partial class WeatherStatusEffectComponent : Component
{
    /// <summary>
    /// A texture that will tile and render as a weather effect across the entire map.
    /// </summary>
    [DataField(required: true)]
    public SpriteSpecifier Sprite = default!;

    // CrystallEdge: optional second sprite layer, drawn under Sprite and clipped to solid (non-space) tiles only
    /// <summary>
    /// An optional second texture, tiled and rendered only on solid weather-affected tiles (not over space/gaps).
    /// Drawn underneath <see cref="Sprite"/>. Uses the same <see cref="Color"/> tint.
    /// </summary>
    [DataField]
    public SpriteSpecifier? GroundSprite;
    // CrystallEdge end

    /// <summary>
    /// Tint that will be applied to the weather texture.
    /// </summary>
    [DataField]
    public Color? Color;

    /// <summary>
    /// Weather scrolling speed.
    /// </summary>
    [DataField]
    public Vector2? Scrolling;

    /// <summary>
    /// Sound to play on the affected areas.
    /// </summary>
    [DataField]
    public SoundSpecifier? Sound;

    /// <summary>
    /// Client audio stream.
    /// Not used on the server.
    /// </summary>
    [ViewVariables]
    public EntityUid? Stream;
}
