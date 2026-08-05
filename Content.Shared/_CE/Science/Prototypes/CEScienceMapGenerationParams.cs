using Robust.Shared.Noise;

namespace Content.Shared._CE.Science.Prototypes;

[DataDefinition]
public sealed partial class CEScienceMapGenerationParams
{
    [DataField]
    public int Radius = 4;

    [DataField]
    public int MinTargetDistance = 2;

    /// <summary>
    /// Noise layers deciding which tiles become dead zones. A tile becomes a dead zone if any
    /// layer's noise value there exceeds its threshold. Sampled at a random per-generation offset
    /// (see CEScienceSystem.Generation.cs) so repeat projects for the same discovery don't always
    /// produce an identical-looking map.
    /// </summary>
    [DataField]
    public List<CEScienceNoiseLayer> DeadZoneLayers = new();
}

[DataRecord]
public partial record struct CEScienceNoiseLayer
{
    /// <summary>
    /// If the noise value at a coordinate is above this, that coordinate becomes a dead zone.
    /// </summary>
    [DataField]
    public float Threshold;

    [DataField(required: true)]
    public FastNoiseLite Noise;
}
