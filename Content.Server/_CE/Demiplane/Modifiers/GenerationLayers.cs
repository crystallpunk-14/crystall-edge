using System.Threading.Tasks;
using Content.Server._CE.Procedural.Generation;
using Robust.Shared.Prototypes;

namespace Content.Server._CE.Demiplane.Modifiers;

/// <summary>
/// Runs <see cref="Layers"/> against every already-generated map, one level at a time.
/// </summary>
public sealed partial class GenerationLayers : ICEDemiplaneModifierEffect
{
    [DataField(required: true)]
    public List<ICEProceduralLayer> Layers = new();

    public async Task Apply(CEProceduralGenerationContext context, Dictionary<int, EntityUid> mapsByHeight, ComponentRegistry components)
    {
        foreach (var (height, map) in mapsByHeight)
        {
            var levelContext = context with { Seed = context.Seed + height };

            foreach (var layer in Layers)
            {
                await layer.Apply(levelContext, map);
                await levelContext.Suspend();
                levelContext.Cancellation.ThrowIfCancellationRequested();
            }
        }
    }
}
