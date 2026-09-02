using System.Threading.Tasks;
using Content.Server._CE.Procedural;
using Content.Server._CE.Procedural.Generation;

namespace Content.Server._CE.Demiplane.Generation;

/// <summary>
/// Generates a stage as a stack of maps, one per key in <see cref="LayersByHeight"/>, running each
/// key's <see cref="ICEProceduralLayer"/>s over its map. The heavy lifting lives in the generic
/// procedural layer runner (<see cref="CEDungeonSystem.GenerateLayers"/>); this is just the
/// demiplane-side adapter onto it.
/// </summary>
public sealed partial class CELayeredWorldGenerator : ICEDemiplaneLocationGenerator
{
    /// <summary>
    /// Layers grouped by stack height. The key is an ordering label — higher = higher up the stack
    /// (nearest the demiplane entry point) — not a physical index, so gaps just mean "no level there".
    /// </summary>
    [DataField(required: true)]
    public Dictionary<int, List<ICEProceduralLayer>> LayersByHeight = new();

    public async Task<List<EntityUid>> Generate(CEProceduralGenerationContext context)
    {
        return await context.EntityManager.System<CEDungeonSystem>().GenerateLayers(context, LayersByHeight);
    }
}
