using System.Threading;
using System.Threading.Tasks;
using Content.Server.Decals;
using Content.Server.Parallax;
using Content.Shared.Light.EntitySystems;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Server._CE.Procedural.Generation;

public sealed class CEProceduralGenerationContext(
    IEntityManager entityManager,
    MapSystem map,
    BiomeSystem biome,
    IPrototypeManager prototype,
    ITileDefinitionManager tileDefManager,
    DecalSystem decals,
    SharedRoofSystem roof,
    int seed,
    Func<ValueTask> suspend,
    CancellationToken cancellation)
{
    public readonly IEntityManager EntityManager = entityManager;
    public readonly MapSystem Map = map;
    public readonly BiomeSystem Biome = biome;
    public readonly IPrototypeManager Prototype = prototype;
    public readonly ITileDefinitionManager TileDefManager = tileDefManager;
    public readonly DecalSystem Decals = decals;
    public readonly SharedRoofSystem Roof = roof;

    public readonly int Seed = seed;

    /// <summary>
    /// Cooperatively yields back to the job queue once this tick's time budget is spent — wraps the
    /// owning <c>Job&lt;T&gt;</c>'s own suspend mechanism. Call this periodically in any loop that
    /// might run long, same as upstream dungeon generation does.
    /// </summary>
    public readonly Func<ValueTask> Suspend = suspend;

    public readonly CancellationToken Cancellation = cancellation;
}
