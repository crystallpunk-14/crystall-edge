using System.Threading.Tasks;
using Content.Server._CE.Procedural.Generation;
using Robust.Shared.Prototypes;

namespace Content.Server._CE.Demiplane.Generation;

[ImplicitDataDefinitionForInheritors]
public partial interface ICEDemiplaneLocationGenerator
{
    Task<CEDemiplaneGenerationResult> Generate(CEProceduralGenerationContext context);
}

public sealed class CEDemiplaneGenerationResult
{
    public required List<EntityUid> Maps;

    public ComponentRegistry Components = new();
}
