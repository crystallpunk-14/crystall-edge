namespace Content.Server._CE.Procedural.Generation;

/// <summary>
/// One generation step, selected in YAML via <c>!type:</c>. Each implementation owns its entire
/// generation logic in its own file; the runner just calls <see cref="Apply"/> on every layer,
/// polymorphically, with no switch over concrete layer types.
/// </summary>
[ImplicitDataDefinitionForInheritors]
public partial interface ICEProceduralLayer
{
    /// <summary>
    /// Applies this layer to <paramref name="map"/>, touching only tiles inside
    /// <see cref="CEProceduralGenerationContext.Region"/>.
    /// </summary>
    void Apply(CEProceduralGenerationContext context, EntityUid map);
}
