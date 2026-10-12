using Content.Server._CE.Procedural.Generation;

namespace Content.Server._CE.Procedural.Wildlands.Effects;

/// <summary>
/// Runs <see cref="Layers"/> against every level, with the seed offset by depth so levels don't mirror each other.
/// </summary>
public sealed partial class GenerationLayers : ICEWildlandsModifierEffect
{
    [DataField(required: true)]
    public List<ICEProceduralLayer> Layers = new();

    public void Apply(CEProceduralGenerationContext context, IReadOnlyDictionary<int, EntityUid> levels)
    {
        foreach (var (depth, map) in levels)
        {
            var levelContext = context with { Seed = context.Seed + depth };

            foreach (var layer in Layers)
            {
                layer.Apply(levelContext, map);
            }
        }
    }
}
