using System.Threading.Tasks;
using Content.Server._CE.Procedural.Generation;
using Robust.Shared.Prototypes;

namespace Content.Server._CE.Demiplane.Modifiers;

/// <summary>
/// Applies itself against the already-generated maps and the shared network components dict.
/// </summary>
[ImplicitDataDefinitionForInheritors]
public partial interface ICEDemiplaneModifierEffect
{
    Task Apply(CEProceduralGenerationContext context, Dictionary<int, EntityUid> mapsByHeight, ComponentRegistry components);
}