using Content.Server._CE.Procedural.Generation;

namespace Content.Server._CE.Demiplane.Modifiers;

/// <summary>
/// Runs <see cref="Layers"/> on every level the location generates. Not wired up to any generator
/// yet — a generator would need to know to ask for and apply these.
/// </summary>
public sealed partial class GenerationLayers : ICEDemiplaneModifierEffect
{
    [DataField(required: true)]
    public List<ICEProceduralLayer> Layers = new();
}
