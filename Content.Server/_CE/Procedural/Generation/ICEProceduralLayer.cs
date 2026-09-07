using System.Threading.Tasks;

namespace Content.Server._CE.Procedural.Generation;

/// <summary>
/// One generation step, selected in YAML via <c>!type:</c>. Each implementation owns its entire
/// generation logic in its own file; the runner just awaits <see cref="Apply"/> on every layer,
/// polymorphically, with no switch over concrete layer types.
/// </summary>
[ImplicitDataDefinitionForInheritors]
public partial interface ICEProceduralLayer
{
    /// <summary>
    /// Applies this layer to its target <paramref name="map"/>. The map is passed by stable
    /// <see cref="EntityUid"/>, never by a list index, so anything that adds or inserts z-levels
    /// mid-generation can't shift a target out from under a later layer.
    /// </summary>
    Task Apply(CEProceduralGenerationContext context, EntityUid map);
}
