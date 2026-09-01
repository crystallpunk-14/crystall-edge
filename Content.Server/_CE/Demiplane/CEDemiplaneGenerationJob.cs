using System.Threading;
using System.Threading.Tasks;
using Content.Server._CE.Demiplane.Generation;
using Content.Server.Parallax;
using Robust.Server.GameObjects;
using Robust.Shared.CPUJob.JobQueues;
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
    private readonly CEDemiplaneGenerationContext _context;
    private readonly ICEDemiplaneLocationGenerator _generator;

    public CEDemiplaneGenerationJob(
        double maxTime,
        IEntityManager entManager,
        MapSystem map,
        BiomeSystem biome,
        IPrototypeManager proto,
        ICEDemiplaneLocationGenerator generator,
        int seed,
        CancellationToken cancellation = default)
        : base(maxTime, cancellation)
    {
        _generator = generator;
        _context = new CEDemiplaneGenerationContext(entManager, map, biome, proto, seed, SuspendIfOutOfTime, cancellation);
    }

    protected override async Task<List<EntityUid>?> Process()
    {
        return await _generator.Generate(_context);
    }
}
