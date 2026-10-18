using Content.Server._CE.Procedural.Generation;

namespace Content.Server._CE.Procedural.Wildlands.Effects;

/// <summary>
/// Runs <see cref="Layers"/> against every level (or only those within <see cref="MinDepth"/>..<see cref="MaxDepth"/>),
/// with the seed offset by depth so levels don't mirror each other.
/// </summary>
public sealed partial class GenerationLayers : ICEWildlandsModifierEffect
{
    [DataField(required: true)]
    public List<ICEProceduralLayer> Layers = new();

    /// <summary>
    /// Optional limits on the depths the layers run on, e.g. caves only.
    /// </summary>
    [DataField]
    public int? MinDepth;

    [DataField]
    public int? MaxDepth;

    public void Apply(CEProceduralGenerationContext context, IReadOnlyDictionary<int, EntityUid> levels)
    {
        foreach (var (depth, map) in levels)
        {
            if (MinDepth is { } min && depth < min || MaxDepth is { } max && depth > max)
                continue;

            var levelContext = context with { Seed = context.Seed + depth };

            foreach (var layer in Layers)
            {
                layer.Apply(levelContext, map);
            }
        }
    }
}
