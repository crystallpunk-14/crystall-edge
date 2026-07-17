using System.Threading;
using Content.Shared._CE.ZLevels.Core.Components;
using Content.Shared._CE.ZLevels.Core.EntitySystems;
using Content.Shared.Destructible;
using Content.Shared.GameTicking;
using Content.Shared.Maps;
using Robust.Shared.CPUJob.JobQueues;
using Robust.Shared.CPUJob.JobQueues.Queues;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Timing;

namespace Content.Server._CE.ZCollapse;

/// <summary>
/// Computes and enforces per-tile structural stability for floating grids: a
/// <see cref="CEGridStabilityCoreComponent"/> seeds its tile, stability flood-fills outward tile by
/// tile losing 1 per hop, and <see cref="CEGridStabilitySupportComponent"/> bridges that flood
/// between a grid and the Z-level directly above it. Any tile whose stability reaches 0 is deleted.
///
/// There is exactly one algorithm: a grid marked dirty gets a full, from-scratch multi-source flood
/// fill (<see cref="StabilityJob"/>) derived directly from whichever Cores/Supports are currently
/// anchored — never an incremental patch of cached seed values. This is deliberate: there is nothing
/// to desync, so a Core disappearing can never leave a stale contribution behind. The flood fill
/// itself runs as a time-sliced <see cref="Robust.Shared.CPUJob.JobQueues.Job{T}"/> (same pattern as
/// dungeon generation) so a large flood or a busy tick never blocks the server — see
/// <see cref="StabilityJob"/>.
///
/// Only grids carrying <see cref="CEGridStabilityComponent"/> participate (opt-in, see that
/// component's docs).
/// </summary>
public sealed partial class CEZCollapseSystem : EntitySystem
{
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private CESharedZLevelsSystem _zLevel = default!;
    [Dependency] private ITileDefinitionManager _tileDefMan = default!;
    [Dependency] private SharedDestructibleSystem _destructible = default!;
    [Dependency] private IGameTiming _timing = default!;

    [Dependency] private EntityQuery<CEGridStabilityComponent> _stabilityQuery = default!;
    [Dependency] private EntityQuery<CEGridStabilityCoreComponent> _coreQuery = default!;
    [Dependency] private EntityQuery<CEGridStabilitySupportComponent> _supportQuery = default!;
    [Dependency] private EntityQuery<MapGridComponent> _gridQuery = default!;
    [Dependency] private EntityQuery<CEZMapComponent> _zMapQuery = default!;
    [Dependency] private EntityQuery<TransformComponent> _xformQuery = default!;

    private const double ZCollapseJobTime = 0.005;

    /// <summary>How long a grid's tiles are protected from reaping after MapInit/grid split. See <see cref="CEGridStabilityComponent.ProtectedUntil"/>.</summary>
    private static readonly TimeSpan StartupProtection = TimeSpan.FromSeconds(5);

    private readonly JobQueue _jobQueue = new(ZCollapseJobTime);
    private readonly Dictionary<EntityUid, (StabilityJob Job, CancellationTokenSource Cts)> _inFlightJobs = new();

    /// <summary>Grids whose Cores/Supports index and/or stability need recomputing next Update().</summary>
    private HashSet<EntityUid> _dirtyGrids = new();

    /// <summary>
    /// Grids awaiting a one-time rebuild of their Cores/Supports index: either just past MapInit
    /// (prototype-anchored entities never raised an incremental anchor event) or just involved in a
    /// grid split (reparented entities may not have raised one either). Deferred to next Update()
    /// rather than handled inline — for MapInit specifically, the grid's own MapInitEvent fires
    /// before its child entities get theirs (breadth-first init order), so scanning immediately would
    /// see none of them; by next tick, init has fully finished.
    /// </summary>
    private HashSet<EntityUid> _pendingIndexScan = new();

    /// <summary>
    /// Grids currently within their <see cref="CEGridStabilityComponent.ProtectedUntil"/> window.
    /// Checked once a tick so each one gets exactly one forced final recompute — and therefore one
    /// real reap pass with fully-settled data — right as its protection lapses, even if nothing else
    /// happened to mark it dirty again by then. See <see cref="ProcessExpiredProtections"/>.
    /// </summary>
    private readonly HashSet<EntityUid> _protectedGrids = new();

    public override void Initialize()
    {
        base.Initialize();

        InitializeEvents();
        InitializeDebug();

        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundCleanup);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        ProcessPendingIndexScans();
        ProcessExpiredProtections();
        StartPendingJobs();
        _jobQueue.Process();
        CollectFinishedJobs();
        PushDirtySnapshots();
    }

    private void OnRoundCleanup(RoundRestartCleanupEvent ev)
    {
        foreach (var (_, cts) in _inFlightJobs.Values)
        {
            cts.Cancel();
            cts.Dispose();
        }

        _inFlightJobs.Clear();
        _dirtyGrids.Clear();
        _pendingIndexScan.Clear();
        _protectedGrids.Clear();
    }

    /// <summary>
    /// Marks a grid protected from reaping until <see cref="StartupProtection"/> from now, and queues
    /// it for the one guaranteed final recompute that lifts that protection — see
    /// <see cref="_protectedGrids"/>.
    /// </summary>
    private void ProtectGrid(EntityUid gridUid, CEGridStabilityComponent comp)
    {
        comp.ProtectedUntil = _timing.CurTime + StartupProtection;
        _protectedGrids.Add(gridUid);
    }

    private void ProcessExpiredProtections()
    {
        if (_protectedGrids.Count == 0)
            return;

        List<EntityUid>? expired = null;
        foreach (var gridUid in _protectedGrids)
        {
            if (!_stabilityQuery.TryGetComponent(gridUid, out var comp) || comp.ProtectedUntil > _timing.CurTime)
                continue;

            expired ??= new List<EntityUid>();
            expired.Add(gridUid);
        }

        if (expired == null)
            return;

        foreach (var gridUid in expired)
        {
            _protectedGrids.Remove(gridUid);
            MarkDirty(gridUid);
        }
    }

    /// <summary>Marks a grid for a full stability recompute next Update(). No-op for non-participating grids.</summary>
    private void MarkDirty(EntityUid gridUid)
    {
        if (_stabilityQuery.HasComponent(gridUid))
            _dirtyGrids.Add(gridUid);
    }

    /// <summary>
    /// Admin escape hatch (<c>znetwork-collapserecalc</c>): forces a grid's stability to recompute.
    /// Identical to what any normal anchor/tile-change event does — there's no separate algorithm
    /// left to "reset" a broken cache with, because there's no cache.
    /// </summary>
    public void ForceRecalculateGrid(EntityUid gridUid)
    {
        MarkDirty(gridUid);
    }

    /// <summary>Marks the Z-level grids directly above and below this one dirty, if they participate.</summary>
    private void MarkZNeighborsDirty(EntityUid gridUid)
    {
        if (!_zMapQuery.TryGetComponent(gridUid, out var zMap))
            return;

        if (_zLevel.TryMapUp((gridUid, zMap), out var above))
            MarkDirty(above.Owner);

        if (_zLevel.TryMapDown((gridUid, zMap), out var below))
            MarkDirty(below.Owner);
    }

    private void ProcessPendingIndexScans()
    {
        if (_pendingIndexScan.Count == 0)
            return;

        var toScan = _pendingIndexScan;
        _pendingIndexScan = new HashSet<EntityUid>();

        foreach (var gridUid in toScan)
        {
            if (!_stabilityQuery.TryGetComponent(gridUid, out var comp))
                continue;

            RescanGridIndex(gridUid, comp);
            MarkDirty(gridUid);
        }
    }

    /// <summary>
    /// Rebuilds a grid's Cores/Supports index from scratch by scanning every anchored Core/Support
    /// in the world. This is a one-time cost (MapInit, grid split) not a per-tick one — the index is
    /// otherwise maintained incrementally in O(1) per anchor/unanchor via <see cref="InitializeEvents"/>.
    /// </summary>
    private void RescanGridIndex(EntityUid gridUid, CEGridStabilityComponent comp)
    {
        comp.Cores.Clear();
        comp.Supports.Clear();

        var coreQuery = AllEntityQuery<CEGridStabilityCoreComponent, TransformComponent>();
        while (coreQuery.MoveNext(out var uid, out _, out var xform))
        {
            if (xform.GridUid == gridUid && xform.Anchored)
                comp.Cores.Add(uid);
        }

        var supportQuery = AllEntityQuery<CEGridStabilitySupportComponent, TransformComponent>();
        while (supportQuery.MoveNext(out var uid, out _, out var xform))
        {
            if (xform.GridUid == gridUid && xform.Anchored)
                comp.Supports.Add(uid);
        }
    }

    private void StartPendingJobs()
    {
        if (_dirtyGrids.Count == 0)
            return;

        var toStart = new List<EntityUid>(_dirtyGrids);
        foreach (var gridUid in toStart)
        {
            // Already recomputing — leave the dirty flag set, it'll be picked up once that job's
            // result has been applied and this loop runs again next Update().
            if (_inFlightJobs.ContainsKey(gridUid))
                continue;

            _dirtyGrids.Remove(gridUid);
            StartJob(gridUid);
        }
    }

    /// <summary>
    /// Snapshots this grid's current ground truth (its own Cores/Supports, plus a read-only copy of
    /// each Z-neighbor's last computed stability) into plain data and queues a <see cref="StabilityJob"/>.
    /// The snapshot itself is O(entities on this one grid) via <see cref="CEGridStabilityComponent.Cores"/>/
    /// <see cref="CEGridStabilityComponent.Supports"/> — never a world-wide scan.
    /// </summary>
    private void StartJob(EntityUid gridUid)
    {
        if (!_stabilityQuery.TryGetComponent(gridUid, out var comp) || !_gridQuery.TryGetComponent(gridUid, out var grid))
            return;

        var liveTiles = new HashSet<Vector2i>();
        var tileEnumerator = _map.GetAllTilesEnumerator(gridUid, grid);
        while (tileEnumerator.MoveNext(out var tileRef))
            liveTiles.Add(tileRef.Value.GridIndices);

        var coreSeeds = new List<(Vector2i, int)>();
        foreach (var coreUid in comp.Cores)
        {
            if (!_coreQuery.TryGetComponent(coreUid, out var core) || !_xformQuery.TryGetComponent(coreUid, out var xform))
                continue;

            coreSeeds.Add((_map.TileIndicesFor(gridUid, grid, xform.Coordinates), core.LevitationForce));
        }

        var ownSupports = new List<(Vector2i, int)>();
        foreach (var supportUid in comp.Supports)
        {
            if (!_supportQuery.TryGetComponent(supportUid, out var support) || !_xformQuery.TryGetComponent(supportUid, out var xform))
                continue;

            ownSupports.Add((_map.TileIndicesFor(gridUid, grid, xform.Coordinates), support.SupportStrength));
        }

        var aboveStability = new Dictionary<Vector2i, int>();
        var belowStability = new Dictionary<Vector2i, int>();
        var belowSupports = new List<(Vector2i, int)>();

        if (_zMapQuery.TryGetComponent(gridUid, out var zMap))
        {
            if (_zLevel.TryMapUp((gridUid, zMap), out var above) && _stabilityQuery.TryGetComponent(above.Owner, out var aboveComp))
                aboveStability = new Dictionary<Vector2i, int>(aboveComp.Stability);

            if (_zLevel.TryMapDown((gridUid, zMap), out var below) &&
                _stabilityQuery.TryGetComponent(below.Owner, out var belowComp) &&
                _gridQuery.TryGetComponent(below.Owner, out var belowGrid))
            {
                belowStability = new Dictionary<Vector2i, int>(belowComp.Stability);

                foreach (var supportUid in belowComp.Supports)
                {
                    if (!_supportQuery.TryGetComponent(supportUid, out var support) || !_xformQuery.TryGetComponent(supportUid, out var xform))
                        continue;

                    belowSupports.Add((_map.TileIndicesFor(below.Owner, belowGrid, xform.Coordinates), support.SupportStrength));
                }
            }
        }

        var cts = new CancellationTokenSource();
        var job = new StabilityJob(ZCollapseJobTime, liveTiles, coreSeeds, ownSupports, belowSupports, aboveStability, belowStability, cts.Token);
        _inFlightJobs[gridUid] = (job, cts);
        _jobQueue.EnqueueJob(job);
    }

    private void CollectFinishedJobs()
    {
        if (_inFlightJobs.Count == 0)
            return;

        List<EntityUid>? finished = null;
        foreach (var (gridUid, entry) in _inFlightJobs)
        {
            if (entry.Job.Status != JobStatus.Finished)
                continue;

            finished ??= new List<EntityUid>();
            finished.Add(gridUid);
        }

        if (finished == null)
            return;

        foreach (var gridUid in finished)
        {
            var (job, cts) = _inFlightJobs[gridUid];
            _inFlightJobs.Remove(gridUid);
            cts.Dispose();
            ApplyJobResult(gridUid, job);
        }
    }

    /// <summary>
    /// Applies a finished job's result: reaps tiles that lost all stability, replaces the grid's
    /// stored <see cref="CEGridStabilityComponent.Stability"/>, and — only if at least one tile's
    /// value actually changed — marks the Z-neighbor grids dirty so a bridge cascade continues.
    /// Gating the cascade on a real diff (rather than unconditionally re-dirtying neighbors) is what
    /// keeps a converged Z-stack from re-triggering itself forever.
    ///
    /// Reaping itself (inside <see cref="ReapDeadTiles"/>) additionally no-ops while the grid is still
    /// within <see cref="CEGridStabilityComponent.ProtectedUntil"/> — stability is still recorded and
    /// still cascades normally either way, so a multi-hop bridge chain keeps settling regardless.
    /// </summary>
    private void ApplyJobResult(EntityUid gridUid, StabilityJob job)
    {
        if (job.Exception != null)
        {
            Log.Error($"ZCollapse: stability job for {ToPrettyString(gridUid)} faulted: {job.Exception}");
            return;
        }

        // The job can take several ticks; the grid or its component may be gone by the time it's done
        // (deleted, Z-network torn down). That's an expected race, not a bug — just drop the result.
        if (!_stabilityQuery.TryGetComponent(gridUid, out var comp) || !_gridQuery.TryGetComponent(gridUid, out var grid))
            return;

        var newStability = job.Result ?? new Dictionary<Vector2i, int>();

        ReapDeadTiles(gridUid, grid, comp, job.LiveTiles, newStability);

        var changed = StabilityDiffers(comp.Stability, newStability);

        comp.Stability.Clear();
        foreach (var (tile, value) in newStability)
            comp.Stability[tile] = value;

        _debugDirtyGrids.Add(gridUid);

        if (changed)
            MarkZNeighborsDirty(gridUid);
    }

    private static bool StabilityDiffers(Dictionary<Vector2i, int> oldStability, Dictionary<Vector2i, int> newStability)
    {
        if (oldStability.Count != newStability.Count)
            return true;

        foreach (var (tile, value) in oldStability)
        {
            if (!newStability.TryGetValue(tile, out var newValue) || newValue != value)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Deletes every tile that was live when the job snapshot was taken but has no stability in the
    /// job's result, destroying whatever's anchored to it first. Skips tiles already gone (removed by
    /// something else while the job was running), indestructible tiles, anything in mapping mode
    /// (map editors previewing a broken layout shouldn't have tiles vanish under them), and anything
    /// still within <see cref="CEGridStabilityComponent.ProtectedUntil"/> (fresh-loaded grid still
    /// settling a cross-Z cascade — see <see cref="ProtectGrid"/>).
    /// </summary>
    private void ReapDeadTiles(EntityUid gridUid, MapGridComponent grid, CEGridStabilityComponent comp, IReadOnlySet<Vector2i> liveTiles, Dictionary<Vector2i, int> newStability)
    {
        if (comp.ProtectedUntil > _timing.CurTime)
            return;

        if (!_map.IsInitialized(Transform(gridUid).MapUid))
            return;

        List<(Vector2i, Tile)>? toDelete = null;
        foreach (var tile in liveTiles)
        {
            if (newStability.ContainsKey(tile))
                continue;

            if (!_map.TryGetTile(grid, tile, out var currentTile) || currentTile.IsEmpty)
                continue;

            if (_tileDefMan[currentTile.TypeId] is ContentTileDefinition { Indestructible: true })
                continue;

            DestroyAnchoredEntities(gridUid, grid, tile);

            toDelete ??= new List<(Vector2i, Tile)>();
            toDelete.Add((tile, Tile.Empty));
        }

        if (toDelete is { Count: > 0 })
            _map.SetTiles(gridUid, grid, toDelete);
    }

    /// <summary>Force-destroys everything anchored to a collapsing tile through the normal Destructible pipeline.</summary>
    private void DestroyAnchoredEntities(EntityUid gridUid, MapGridComponent grid, Vector2i tile)
    {
        var enumerator = _map.GetAnchoredEntitiesEnumerator(gridUid, grid, tile);
        List<EntityUid>? anchored = null;
        while (enumerator.MoveNext(out var ent))
        {
            anchored ??= new List<EntityUid>();
            anchored.Add(ent.Value);
        }

        if (anchored == null)
            return;

        foreach (var uid in anchored)
        {
            _destructible.DestroyEntity(uid);
        }
    }
}
