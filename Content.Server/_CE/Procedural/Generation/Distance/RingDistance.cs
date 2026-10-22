namespace Content.Server._CE.Procedural.Generation.Distance;

/// <summary>
/// Falloff that is lowest on a ring around the center and rises both inward and outward — pushes
/// features away from the center (e.g. a city) without dragging them out to the very edge.
/// </summary>
public sealed partial class RingDistance : ICEDistanceConfig
{
    [DataField]
    public float BlendWeight { get; set; } = 0.5f;

    /// <summary>
    /// Ring radius, in the same -1..1 units as the sampled position (1 = half the layer size).
    /// </summary>
    [DataField(required: true)]
    public float Radius;

    /// <summary>
    /// How far from the ring, in the same units, the falloff reaches 1.
    /// </summary>
    [DataField(required: true)]
    public float Width;

    public float GetDistance(float dx, float dy)
        => MathF.Min(1f, MathF.Abs(MathF.Sqrt(dx * dx + dy * dy) - Radius) / Width);
}
