using Robust.Shared.Random;

namespace Content.Server._CE.Procedural.Generation.Points;

/// <summary>
/// Shape a <see cref="CEProceduralPoint"/> picks its positions from — selected in YAML via <c>!type:</c>.
/// </summary>
[ImplicitDataDefinitionForInheritors]
public partial interface ICEPointArea
{
    /// <summary>
    /// Random offset inside the shape, relative to the generation center.
    /// </summary>
    Vector2i Sample(IRobustRandom random);
}
