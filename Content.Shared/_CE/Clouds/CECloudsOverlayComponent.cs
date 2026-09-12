using System.Numerics;
using Robust.Shared.GameStates;

namespace Content.Shared._CE.Clouds;

/// <summary>
/// Added to a map entity (typically via a station's zLevelsComponentOverrides) to make
/// <see cref="Content.Client._CE.Clouds.CECloudsOverlay"/> draw drifting, evolving
/// cloud shadows over that map. Only darkens the map's ambient light baseline
/// (<see cref="Robust.Shared.Map.Components.MapLightComponent"/>) - point lights are unaffected.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CECloudsOverlayComponent : Component
{
    /// <summary>
    /// Seed offsetting the noise domain so different maps can show differently-shaped clouds.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int Seed;

    /// <summary>
    /// World-space noise sampling frequency. Higher values produce smaller cloud shapes.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Frequency = 0.02f;

    /// <summary>
    /// Number of fractal noise layers summed together. More octaves add finer detail at a GPU cost.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int Octaves = 3;

    /// <summary>
    /// Frequency multiplier applied to each successive octave.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Lacunarity = 2f;

    /// <summary>
    /// Amplitude multiplier applied to each successive octave.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Gain = 0.5f;

    /// <summary>
    /// World-space drift velocity of the cloud pattern (direction and speed combined).
    /// </summary>
    [DataField, AutoNetworkedField]
    public Vector2 WindVelocity = new(1f, 0.4f);

    /// <summary>
    /// How fast the cloud shapes morph over time (the noise's pseudo-3rd dimension).
    /// </summary>
    [DataField, AutoNetworkedField]
    public float EvolutionSpeed = 0.05f;

    /// <summary>
    /// Noise threshold above which a pixel counts as "under a cloud". Higher values mean less cloud cover.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Coverage = 0.5f;

    /// <summary>
    /// Maximum alpha of the shadow at the densest part of a cloud.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float ShadowStrength = 0.4f;

    /// <summary>
    /// Color blended in for shadowed pixels.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Color ShadowColor = Color.Black;
}
