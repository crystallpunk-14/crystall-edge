using System.Threading;
using System.Threading.Tasks;
using Robust.Shared.CPUJob.JobQueues;
using Robust.Shared.Map;

namespace Content.Server._CE.ZCollapse;

/// <summary>
/// Pure-data multi-source flood fill for one grid's stability. Everything it needs is captured as a
/// plain-data snapshot on the main thread before the job is queued (see
/// <see cref="CEZCollapseSystem"/>) — <see cref="Process"/> never touches <c>IEntityManager</c> or
/// any system, so it's safe to suspend and resume across many ticks even while the live world keeps
/// changing underneath it. A grid re-dirtied mid-job is simply re-queued for another pass once this
/// one's (slightly stale) result has been applied — see <see cref="CEZCollapseSystem"/> for that.
/// </summary>
public sealed class StabilityJob : Job<Dictionary<Vector2i, int>>
{
    /// <summary>Tiles that physically exist on this grid right now. Flood fill never crosses into a tile absent here.</summary>
    private readonly HashSet<Vector2i> _liveTiles;

    /// <summary>Exposed so the completion handler can reap tiles that ended up with no stability without re-enumerating the grid.</summary>
    public IReadOnlySet<Vector2i> LiveTiles => _liveTiles;

    /// <summary>(tile, LevitationForce) for every Core currently anchored on this grid.</summary>
    private readonly List<(Vector2i Tile, int Value)> _coreSeeds;

    /// <summary>(tile, SupportStrength) for every Support anchored on this grid — bridges down from the level above.</summary>
    private readonly List<(Vector2i Tile, int Strength)> _ownSupports;

    /// <summary>(tile, SupportStrength) for every Support anchored on the grid directly below this one — bridges up into this grid.</summary>
    private readonly List<(Vector2i Tile, int Strength)> _belowSupports;

    /// <summary>Snapshot of the Z-level above's last computed stability, used to cap <see cref="_ownSupports"/>.</summary>
    private readonly Dictionary<Vector2i, int> _aboveStability;

    /// <summary>Snapshot of the Z-level below's last computed stability, used to cap <see cref="_belowSupports"/>.</summary>
    private readonly Dictionary<Vector2i, int> _belowStability;

    private static readonly Vector2i[] CardinalOffsets =
    {
        new(1, 0), new(-1, 0), new(0, 1), new(0, -1),
    };

    public StabilityJob(
        double maxTime,
        HashSet<Vector2i> liveTiles,
        List<(Vector2i Tile, int Value)> coreSeeds,
        List<(Vector2i Tile, int Strength)> ownSupports,
        List<(Vector2i Tile, int Strength)> belowSupports,
        Dictionary<Vector2i, int> aboveStability,
        Dictionary<Vector2i, int> belowStability,
        CancellationToken cancellation = default) : base(maxTime, cancellation)
    {
        _liveTiles = liveTiles;
        _coreSeeds = coreSeeds;
        _ownSupports = ownSupports;
        _belowSupports = belowSupports;
        _aboveStability = aboveStability;
        _belowStability = belowStability;
    }

    protected override async Task<Dictionary<Vector2i, int>?> Process()
    {
        var stability = new Dictionary<Vector2i, int>();
        var queue = new Queue<(Vector2i Tile, int Value)>();

        foreach (var (tile, value) in _coreSeeds)
        {
            Seed(stability, queue, tile, value);
        }

        foreach (var (tile, strength) in _ownSupports)
        {
            var donor = _aboveStability.GetValueOrDefault(tile, 0);
            if (donor > 0)
                Seed(stability, queue, tile, Math.Min(strength, donor));
        }

        foreach (var (tile, strength) in _belowSupports)
        {
            var donor = _belowStability.GetValueOrDefault(tile, 0);
            if (donor > 0)
                Seed(stability, queue, tile, Math.Min(strength, donor));
        }

        var visited = 0;
        while (queue.TryDequeue(out var entry))
        {
            var (tile, value) = entry;

            foreach (var offset in CardinalOffsets)
            {
                var neighbor = tile + offset;
                if (!_liveTiles.Contains(neighbor))
                    continue;

                var next = value - 1;
                if (next <= 0)
                    continue;

                if (next > stability.GetValueOrDefault(neighbor, 0))
                {
                    stability[neighbor] = next;
                    queue.Enqueue((neighbor, next));
                }
            }

            // Only check the clock every so often — StopWatch reads are cheap but not free, and this
            // keeps the common case (a small/moderate grid) from paying for a check it'll never need.
            if (++visited % 256 == 0)
                await SuspendIfOutOfTime();
        }

        return stability;
    }

    private void Seed(Dictionary<Vector2i, int> stability, Queue<(Vector2i, int)> queue, Vector2i tile, int value)
    {
        if (value <= 0 || !_liveTiles.Contains(tile))
            return;

        if (value > stability.GetValueOrDefault(tile, 0))
        {
            stability[tile] = value;
            queue.Enqueue((tile, value));
        }
    }
}
