using System.Threading;
using System.Threading.Tasks;
using Content.Server._CE.Demiplane.Generation;
using Content.Server._CE.Procedural.Generation;
using Content.Server.Decals;
using Content.Server.Parallax;
using Robust.Server.GameObjects;
using Robust.Shared.CPUJob.JobQueues;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Server._CE.Demiplane;

/// <summary>
/// Runs one <see cref="ICEDemiplaneLocationGenerator"/> off the main thread's time budget via
/// <see cref="Job{T}"/>, same pattern as upstream salvage expeditions. Owns no generation logic
/// itself — that lives entirely in the generator (see <see cref="ICEDemiplaneLocationGenerator"/>),
/// this just wires up the context and awaits it.
/// </summary>
public sealed class CEDemiplaneGenerationJob : Job<List<EntityUid>>
{
    private readonly CEProceduralGenerationContext _context;
    private readonly ICEDemiplaneLocationGenerator _generator;

    public CEDemiplaneGenerationJob(
        double maxTime,
        IEntityManager entManager,
        MapSystem map,
        BiomeSystem biome,
        IPrototypeManager proto,
        ITileDefinitionManager tileDefManager,
        DecalSystem decals,
        ICEDemiplaneLocationGenerator generator,
        int seed,
        CancellationToken cancellation = default)
        : base(maxTime, cancellation)
    {
        _generator = generator;
        _context = new CEProceduralGenerationContext(entManager, map, biome, proto, tileDefManager, decals, seed, SuspendIfOutOfTime, cancellation);
    }

    protected override async Task<List<EntityUid>?> Process()
    {
        return await _generator.Generate(_context);
    }
}
