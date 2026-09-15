using System.Threading;
using System.Threading.Tasks;
using Content.Server._CE.Demiplane.Generation;
using Content.Server._CE.Procedural;
using Content.Server._CE.Procedural.Generation;
using Content.Server.Decals;
using Content.Server.Parallax;
using Content.Shared.Light.EntitySystems;
using Robust.Server.GameObjects;
using Robust.Shared.CPUJob.JobQueues;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Server._CE.Demiplane;

public sealed class CEDemiplaneGenerationJob : Job<CEDemiplaneGenerationResult>
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
        SharedRoofSystem roof,
        ICEDemiplaneLocationGenerator generator,
        int seed,
        int difficulty,
        CancellationToken cancellation = default)
        : base(maxTime, cancellation)
    {
        _generator = generator;
        _context = new CEProceduralGenerationContext(entManager, map, biome, proto, tileDefManager, decals, roof, seed, difficulty, SuspendIfOutOfTime, cancellation);
    }

    protected override async Task<CEDemiplaneGenerationResult?> Process()
    {
        var result = await _generator.Generate(_context);

        // Empty gap level between the island and the generated stage, so they don't sit flush.
        var dungeon = _context.EntityManager.System<CEDungeonSystem>();
        result.Maps.Insert(0, dungeon.LoadMap());

        return result;
    }
}
