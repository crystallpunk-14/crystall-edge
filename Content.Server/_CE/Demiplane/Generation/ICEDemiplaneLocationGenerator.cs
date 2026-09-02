using System.Threading.Tasks;
using Content.Server._CE.Procedural.Generation;

namespace Content.Server._CE.Demiplane.Generation;

/// <summary>
/// A demiplane location's generation strategy — selected in YAML via <c>!type:</c> on
/// <see cref="Prototypes.CEDemiplaneLocationPrototype.Generator"/>. Each implementation owns its
/// entire generation logic in its own file; <see cref="CEDemiplaneGenerationJob"/> just awaits
/// <see cref="Generate"/> polymorphically and never needs a switch over concrete generator types.
/// A generator always produces a multi-level map — one or more z-levels stacked below the
/// demiplane entry point — never a single flat map.
/// </summary>
[ImplicitDataDefinitionForInheritors]
public partial interface ICEDemiplaneLocationGenerator
{
    /// <summary>
    /// Generates the location. Returns the freshly created maps, nearest-to-entry-point first.
    /// None of them are attached to any z-network yet — that is the caller's job, once this
    /// returns and whatever else it is waiting on (a teleport timer, say) is also done.
    /// </summary>
    Task<List<EntityUid>> Generate(CEProceduralGenerationContext context);
}
