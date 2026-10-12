namespace Content.Server._CE.Procedural.Generation.Layers;

/// <summary>
/// Runs <see cref="Layers"/> once per position of the named <see cref="Point"/>, re-centered on it — e.g. a
/// sub-biome painted somewhere off the main island. Every center-relative layer and mask follows the shift with
/// no changes of its own. Each position gets its own seed (by index, so the same position keeps the same seed on
/// every z-level) to keep several copies from looking identical.
/// </summary>
public sealed partial class AtPointLayer : ICEProceduralLayer
{
    private static readonly ISawmill Sawmill = Logger.GetSawmill("ce.procedural");

    [DataField(required: true)]
    public string Point = string.Empty;

    [DataField(required: true)]
    public List<ICEProceduralLayer> Layers = new();

    public void Apply(CEProceduralGenerationContext context, EntityUid map)
    {
        if (!context.Points.TryGetValue(Point, out var positions))
        {
            Sawmill.Error($"{nameof(AtPointLayer)} references unknown point '{Point}'.");
            return;
        }

        for (var i = 0; i < positions.Count; i++)
        {
            var pointContext = context with { Center = positions[i], Seed = context.Seed + i * 7919 };
            foreach (var layer in Layers)
            {
                layer.Apply(pointContext, map);
            }
        }
    }
}
