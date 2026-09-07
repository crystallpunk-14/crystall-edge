using System.Threading.Tasks;
using Content.Server._CE.Procedural.Generation;
using Robust.Shared.Prototypes;

namespace Content.Server._CE.Demiplane.Modifiers;

/// <summary>
/// Merges <see cref="Components"/> into the shared network components dict.
/// </summary>
public sealed partial class AddZNetworkComponents : ICEDemiplaneModifierEffect
{
    [DataField(required: true)]
    public ComponentRegistry Components = new();

    public Task Apply(CEProceduralGenerationContext context, Dictionary<int, EntityUid> mapsByHeight, ComponentRegistry components)
    {
        foreach (var (name, entry) in Components)
        {
            components[name] = entry;
        }

        return Task.CompletedTask;
    }
}
