using System.Threading;
using System.Threading.Tasks;
using Content.Server.Decals;
using Content.Server.Parallax;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Server._CE.Procedural.Generation;

/// <summary>
/// Everything an <see cref="ICEProceduralLayer"/> needs to run, bundled so the interface method
/// takes one parameter regardless of what a given layer actually uses. Also the context passed to
/// demiplane location generators — they share this one type rather than each subsystem defining its
/// own field-identical copy.
/// </summary>
public sealed class CEProceduralGenerationContext(
    IEntityManager entityManager,
    MapSystem map,
    BiomeSystem biome,
    IPrototypeManager prototype,
    ITileDefinitionManager tileDefManager,
    DecalSystem decals,
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

    public readonly int Seed = seed;

    /// <summary>
    /// Cooperatively yields back to the job queue once this tick's time budget is spent — wraps the
    /// owning <c>Job&lt;T&gt;</c>'s own suspend mechanism. Call this periodically in any loop that
    /// might run long, same as upstream dungeon generation does.
    /// </summary>
    public readonly Func<ValueTask> Suspend = suspend;

    public readonly CancellationToken Cancellation = cancellation;
}
