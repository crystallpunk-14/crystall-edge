namespace Content.Server._CE.Procedural.Generation.Distance;

/// <summary>
/// Radial falloff shape blended into noise to confine it into a bounded, island-like area — selected
/// in YAML via <c>!type:</c>. Each shape computes its own <see cref="GetDistance"/> so there is no
/// switch over shape types anywhere (unlike upstream's <c>DungeonJob.GetDistance</c>).
/// </summary>
[ImplicitDataDefinitionForInheritors]
public partial interface ICEDistanceConfig
{
    /// <summary>
    /// How much a sampled point is blended from raw noise toward <c>1 - GetDistance</c>. 0 = pure
    /// noise (ignores the center), 1 = pure radial falloff (a clean shape with no noisy edge).
    /// </summary>
    float BlendWeight { get; }

    /// <summary>
    /// Falloff at a point, where <paramref name="dx"/>/<paramref name="dy"/> are its position in
    /// the range -1..1 relative to the area's center. Returns 0 at the center rising toward 1 at the
    /// edge.
    /// </summary>
    float GetDistance(float dx, float dy);
}
