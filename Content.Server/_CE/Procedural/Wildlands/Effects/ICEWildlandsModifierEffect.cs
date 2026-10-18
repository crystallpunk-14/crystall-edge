using Content.Server._CE.Procedural.Generation;

namespace Content.Server._CE.Procedural.Wildlands.Effects;

[ImplicitDataDefinitionForInheritors]
public partial interface ICEWildlandsModifierEffect
{
    /// <summary>
    /// Applies the effect to every level (keyed by depth), touching only tiles inside the context's region.
    /// </summary>
    void Apply(CEProceduralGenerationContext context, IReadOnlyDictionary<int, EntityUid> levels);
}
