using System.Threading;
using System.Threading.Tasks;
using Content.Server.Parallax;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Server._CE.Demiplane.Generation;

/// <summary>
/// Everything an <see cref="ICEDemiplaneLocationGenerator"/> needs to run, bundled so the interface
/// method takes one parameter regardless of what a given generator actually uses.
/// </summary>
public sealed class CEDemiplaneGenerationContext(
    IEntityManager entityManager,
    MapSystem map,
    BiomeSystem biome,
    IPrototypeManager prototype,
    int seed,
    Func<ValueTask> suspend,
    CancellationToken cancellation)
{
    public readonly IEntityManager EntityManager = entityManager;
    public readonly MapSystem Map = map;
    public readonly BiomeSystem Biome = biome;
    public readonly IPrototypeManager Prototype = prototype;

    public readonly int Seed = seed;

    /// <summary>
    /// Cooperatively yields back to the job queue once this tick's time budget is spent — wraps the
    /// owning <c>Job&lt;T&gt;</c>'s own suspend mechanism. Call this periodically in any loop that
    /// might run long, same as upstream dungeon generation does.
    /// </summary>
    public readonly Func<ValueTask> Suspend = suspend;

    public readonly CancellationToken Cancellation = cancellation;
}
