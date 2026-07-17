using System.Threading;
using System.Threading.Tasks;
using Robust.Shared.Map;
using Robust.Shared.CPUJob.JobQueues;

namespace Content.Server._CE.ZCollapse;

/// <summary>
/// Pure-data multi-source flood fill across one whole Z-column — every grid bridged to every other
/// via a chain of <see cref="CEGridStabilitySupportComponent"/>s, computed together in a single pass
/// rather than grid-by-grid. This matters for correctness, not just convenience: if each grid were
/// computed separately and capped its Support seeds against a neighbor grid's *previously stored*
/// result, two Supports facing each other across a boundary can reach a stable mutual fixed point
/// (grid A reads B's old value, B reads A's new value, neither ever decreasing) and hold a whole
/// section aloft with zero Cores anywhere — the exact "stability from nothing" bug the old
/// incremental algorithm also had to specifically guard against. Doing the whole column as one
/// unified BFS makes that structurally impossible instead: a node can only ever get a value by being
/// reachable, within this one pass, from an actual Core seed — nothing to reach back to means nothing
/// ever enters the queue for it, full stop.
///
/// Everything this needs is captured as a plain-data snapshot on the main thread before the job is
/// queued (see <see cref="CEZCollapseSystem"/>) — <see cref="Process"/> never touches
/// <c>IEntityManager</c> or any system, so it's safe to suspend and resume across many ticks even
/// while the live world keeps changing underneath it.
/// </summary>
public sealed class StabilityJob : Job<Dictionary<(EntityUid Grid, Vector2i Tile), int>>
{
    /// <summary>Every (grid, tile) that physically exists right now, across every grid in the column.</summary>
    private readonly HashSet<(EntityUid Grid, Vector2i Tile)> _liveNodes;

    public IReadOnlySet<(EntityUid Grid, Vector2i Tile)> LiveNodes => _liveNodes;

    /// <summary>(grid, tile, LevitationForce) for every Core anchored anywhere in the column.</summary>
    private readonly List<(EntityUid Grid, Vector2i Tile, int Value)> _coreSeeds;

    /// <summary>
    /// Symmetric cross-grid edges from Supports: node -&gt; list of (partner node one Z-level away,
    /// that Support's strength cap). Built once up front so the BFS below can treat a Support bridge
    /// exactly like a same-grid neighbor edge, just capping instead of decrementing.
    /// </summary>
    private readonly Dictionary<(EntityUid, Vector2i), List<((EntityUid Grid, Vector2i Tile) Node, int Strength)>> _bridges;

    private static readonly Vector2i[] CardinalOffsets =
    {
        new(1, 0), new(-1, 0), new(0, 1), new(0, -1),
    };

    public StabilityJob(
        double maxTime,
        HashSet<(EntityUid Grid, Vector2i Tile)> liveNodes,
        List<(EntityUid Grid, Vector2i Tile, int Value)> coreSeeds,
        Dictionary<(EntityUid, Vector2i), List<((EntityUid Grid, Vector2i Tile) Node, int Strength)>> bridges,
        CancellationToken cancellation = default) : base(maxTime, cancellation)
    {
        _liveNodes = liveNodes;
        _coreSeeds = coreSeeds;
        _bridges = bridges;
    }

    protected override async Task<Dictionary<(EntityUid Grid, Vector2i Tile), int>?> Process()
    {
        var stability = new Dictionary<(EntityUid, Vector2i), int>();
        var queue = new Queue<((EntityUid Grid, Vector2i Tile) Node, int Value)>();

        foreach (var (grid, tile, value) in _coreSeeds)
        {
            Seed(stability, queue, (grid, tile), value);
        }

        var visited = 0;
        while (queue.TryDequeue(out var entry))
        {
            var (node, value) = entry;

            foreach (var offset in CardinalOffsets)
            {
                var neighbor = (node.Grid, node.Tile + offset);
                Seed(stability, queue, neighbor, value - 1);
            }

            if (_bridges.TryGetValue(node, out var partners))
            {
                foreach (var (partner, strength) in partners)
                {
                    Seed(stability, queue, partner, Math.Min(value, strength));
                }
            }

            // Only check the clock every so often — StopWatch reads are cheap but not free, and this
            // keeps the common case (a small/moderate column) from paying for a check it'll never need.
            if (++visited % 256 == 0)
                await SuspendIfOutOfTime();
        }

        return stability;
    }

    private void Seed(Dictionary<(EntityUid, Vector2i), int> stability, Queue<((EntityUid, Vector2i), int)> queue, (EntityUid Grid, Vector2i Tile) node, int value)
    {
        if (value <= 0 || !_liveNodes.Contains(node))
            return;

        if (value > stability.GetValueOrDefault(node, 0))
        {
            stability[node] = value;
            queue.Enqueue((node, value));
        }
    }
}
