namespace Content.Server._CE.Procedural.Generation.Distance;

/// <summary>
/// Produces a rounder shape, useful for more natural areas.
/// </summary>
public sealed partial class EuclideanSquaredDistance : ICEDistanceConfig
{
    [DataField]
    public float BlendWeight { get; set; } = 0.5f;

    public float GetDistance(float dx, float dy)
        => MathF.Min(1f, (dx * dx + dy * dy) / MathF.Sqrt(2));
}
