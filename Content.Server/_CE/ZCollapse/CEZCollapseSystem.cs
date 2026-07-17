using System.Threading;
using Content.Shared._CE.ZLevels.Core.Components;
using Content.Shared._CE.ZLevels.Core.EntitySystems;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Destructible;
using Content.Shared.FixedPoint;
using Content.Shared.GameTicking;
using Content.Shared.Maps;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.CPUJob.JobQueues;
using Robust.Shared.CPUJob.JobQueues.Queues;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._CE.ZCollapse;

public sealed partial class CEZCollapseSystem : EntitySystem
{
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private CESharedZLevelsSystem _zLevel = default!;
    [Dependency] private ITileDefinitionManager _tileDefMan = default!;
    [Dependency] private SharedDestructibleSystem _destructible = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedPhysicsSystem _physics = default!;
    [Dependency] private SharedAudioSystem _audio = default!;

    [Dependency] private EntityQuery<CEGridStabilityComponent> _stabilityQuery = default!;
    [Dependency] private EntityQuery<CEGridStabilityCoreComponent> _coreQuery = default!;
    [Dependency] private EntityQuery<CEGridStabilitySupportComponent> _supportQuery = default!;
    [Dependency] private EntityQuery<MapGridComponent> _gridQuery = default!;
    [Dependency] private EntityQuery<CEZMapComponent> _zMapQuery = default!;
    [Dependency] private EntityQuery<TransformComponent> _xformQuery = default!;
    [Dependency] private EntityQuery<DamageableComponent> _damageableQuery = default!;

    private const double ZCollapseJobTime = 0.005;

    /// <summary>
    /// Overwhelming, resistance-ignoring damage used to force-destroy something structurally rather
    /// than silently deleting it — this is what makes a collapsing wall actually run its own
    /// Destructible thresholds (rubble, material refund, break sound) the same way an explosion or a
    /// shuttle impact does, instead of just vanishing. See <see cref="DestroyAnchoredEntities"/>.
    /// </summary>
    private static readonly DamageSpecifier CollapseDamage = new()
    {
        DamageDict = { ["Blunt"] = FixedPoint2.New(1000), ["Structural"] = FixedPoint2.New(1000) },
    };

    private static readonly EntProtoId CollapseDustEffect = "CEDustTileEffect";

    private static readonly SoundSpecifier CollapseSound = new SoundPathSpecifier("/Audio/Magic/rumble.ogg");

    private const float CollapseDelayMinSeconds = 3f;
    private const float CollapseDelayMaxSeconds = 10f;

    //Small random scatter impulse given to debris dropped onto the level below
    //just enough to keep a pile of fallen tile items from stacking in a perfect grid.
    private const float DropImpulseMin = 0f;
    private const float DropImpulseMax = 1.5f;

    private readonly JobQueue _jobQueue = new(ZCollapseJobTime);

    /// <summary>
    /// In-flight column jobs: the job itself, its cancellation source, and every grid it covers.
    /// </summary>
    private readonly List<(StabilityJob Job, CancellationTokenSource Cts, List<EntityUid> Grids)> _inFlightJobs = new();

    /// <summary>
    /// Every grid currently covered by some in-flight job — checked before starting a new one for the same znetwork.
    /// </summary>
    private readonly HashSet<EntityUid> _busyGrids = new();

    /// <summary>
    /// Grids whose column needs recomputing next Update().
    /// </summary>
    private readonly HashSet<EntityUid> _dirtyGrids = new();

    /// <summary>
    /// Grids awaiting a one-time rebuild of their Cores/Supports index: either just past MapInit
    /// (prototype-anchored entities never raised an incremental anchor event) or just involved in a
    /// grid split (reparented entities may not have raised one either). Deferred to next Update()
    /// rather than handled inline — for MapInit specifically, the grid's own MapInitEvent fires
    /// before its child entities get theirs (breadth-first init order), so scanning immediately would
    /// see none of them; by next tick, init has fully finished. This runs before jobs are started each
    /// Update(), so by the time a column is gathered every grid in it already has a correct index —
    /// there's no multi-tick staleness window to wait out.
    /// </summary>
    private HashSet<EntityUid> _pendingIndexScan = new();

    public override void Initialize()
    {
        base.Initialize();

        InitializeEvents();

        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundCleanup);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        ProcessPendingIndexScans();
        StartPendingJobs();
        _jobQueue.Process();
        CollectFinishedJobs();
        ProcessPendingCollapses();
        PushDirtySnapshots();
    }

    private void OnRoundCleanup(RoundRestartCleanupEvent ev)
    {
        foreach (var (_, cts, _) in _inFlightJobs)
        {
            cts.Cancel();
            cts.Dispose();
        }

        _inFlightJobs.Clear();
        _busyGrids.Clear();
        _dirtyGrids.Clear();
        _pendingIndexScan.Clear();
    }

    /// <summary>Marks a grid's column for a full stability recompute next Update(). No-op for non-participating grids.</summary>
    private void MarkDirty(EntityUid gridUid)
    {
        if (_stabilityQuery.HasComponent(gridUid))
            _dirtyGrids.Add(gridUid);
    }

    /// <summary>
    /// Admin escape hatch (<c>znetwork-collapserecalc</c>): forces a grid's column to recompute.
    /// Identical to what any normal anchor/tile-change event does — there's no separate algorithm
    /// left to "reset" a broken cache with, because there's no cache.
    /// </summary>
    public void ForceRecalculateGrid(EntityUid gridUid)
    {
        MarkDirty(gridUid);
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

    /// <summary>
    /// Walks up and down from a grid via Z-level links, collecting every Z-adjacent grid that also
    /// carries <see cref="CEGridStabilityComponent"/> — the chain stops the moment a neighbor doesn't
    /// exist or doesn't participate. Since each grid has at most one map above and below, this is
    /// always a simple ordered chain, never a branching graph.
    /// </summary>
    private List<EntityUid> GetColumn(EntityUid startGrid)
    {
        var above = new List<EntityUid>();
        var current = startGrid;
        while (_zMapQuery.TryGetComponent(current, out var zMap) &&
               _zLevel.TryMapUp((current, zMap), out var up) &&
               _stabilityQuery.HasComponent(up.Owner))
        {
            above.Add(up.Owner);
            current = up.Owner;
        }

        var below = new List<EntityUid>();
        current = startGrid;
        while (_zMapQuery.TryGetComponent(current, out var zMap) &&
               _zLevel.TryMapDown((current, zMap), out var down) &&
               _stabilityQuery.HasComponent(down.Owner))
        {
            below.Add(down.Owner);
            current = down.Owner;
        }

        above.Reverse();
        above.Add(startGrid);
        above.AddRange(below);
        return above;
    }

    private void StartPendingJobs()
    {
        if (_dirtyGrids.Count == 0)
            return;

        var toStart = new List<EntityUid>(_dirtyGrids);
        foreach (var gridUid in toStart)
        {
            // Already consumed by an earlier column in this same pass, or busy in an in-flight job —
            // leave it dirty and pick it up again once that job's result has been applied.
            if (!_dirtyGrids.Contains(gridUid) || _busyGrids.Contains(gridUid))
                continue;

            var column = GetColumn(gridUid);

            var columnBusy = false;
            foreach (var g in column)
            {
                if (_busyGrids.Contains(g))
                {
                    columnBusy = true;
                    break;
                }
            }

            if (columnBusy)
                continue;

            foreach (var g in column)
            {
                _dirtyGrids.Remove(g);
            }

            StartJob(column);
        }
    }

    /// <summary>
    /// Snapshots an entire column's ground truth (every participating grid's Cores/Supports and live
    /// tiles) into plain data and queues one <see cref="StabilityJob"/> covering all of it. The
    /// snapshot is O(entities in this column) via each grid's <see cref="CEGridStabilityComponent.Cores"/>/
    /// <see cref="CEGridStabilityComponent.Supports"/> index — never a world-wide scan.
    /// </summary>
    private void StartJob(List<EntityUid> column)
    {
        var aboveOf = new Dictionary<EntityUid, EntityUid>();
        foreach (var gridUid in column)
        {
            if (_zMapQuery.TryGetComponent(gridUid, out var zMap) && _zLevel.TryMapUp((gridUid, zMap), out var above))
                aboveOf[gridUid] = above.Owner;
        }

        var liveNodes = new HashSet<(EntityUid, Vector2i)>();
        var coreSeeds = new List<(EntityUid, Vector2i, int)>();
        var bridges = new Dictionary<(EntityUid, Vector2i), List<((EntityUid Grid, Vector2i Tile) Node, int Strength, int Loss)>>();

        foreach (var gridUid in column)
        {
            if (!_stabilityQuery.TryGetComponent(gridUid, out var comp) || !_gridQuery.TryGetComponent(gridUid, out var grid))
                continue;

            var tileEnumerator = _map.GetAllTilesEnumerator(gridUid, grid);
            while (tileEnumerator.MoveNext(out var tileRef))
            {
                liveNodes.Add((gridUid, tileRef.Value.GridIndices));
            }

            foreach (var coreUid in comp.Cores)
            {
                if (!_coreQuery.TryGetComponent(coreUid, out var core) || !_xformQuery.TryGetComponent(coreUid, out var xform))
                    continue;

                coreSeeds.Add((gridUid, _map.TileIndicesFor(gridUid, grid, xform.Coordinates), core.LevitationForce));
            }

            if (!aboveOf.TryGetValue(gridUid, out var aboveGrid))
                continue; // no participating neighbor above within this column — nothing to bridge from here

            foreach (var supportUid in comp.Supports)
            {
                if (!_supportQuery.TryGetComponent(supportUid, out var support) || !_xformQuery.TryGetComponent(supportUid, out var xform))
                    continue;

                var tile = _map.TileIndicesFor(gridUid, grid, xform.Coordinates);
                AddBridge(bridges, (gridUid, tile), (aboveGrid, tile), support.SupportStrength, support.TransferLoss);
            }
        }

        var cts = new CancellationTokenSource();
        var job = new StabilityJob(ZCollapseJobTime, liveNodes, coreSeeds, bridges, cts.Token);

        foreach (var gridUid in column)
        {
            _busyGrids.Add(gridUid);
        }

        _inFlightJobs.Add((job, cts, column));
        _jobQueue.EnqueueJob(job);
    }

    private static void AddBridge(
        Dictionary<(EntityUid, Vector2i), List<((EntityUid Grid, Vector2i Tile) Node, int Strength, int Loss)>> bridges,
        (EntityUid, Vector2i) a,
        (EntityUid, Vector2i) b,
        int strength,
        int loss)
    {
        if (!bridges.TryGetValue(a, out var listA))
            bridges[a] = listA = new List<((EntityUid, Vector2i), int, int)>();
        listA.Add((b, strength, loss));

        if (!bridges.TryGetValue(b, out var listB))
            bridges[b] = listB = new List<((EntityUid, Vector2i), int, int)>();
        listB.Add((a, strength, loss));
    }

    private void CollectFinishedJobs()
    {
        if (_inFlightJobs.Count == 0)
            return;

        List<int>? finishedIndices = null;
        for (var i = 0; i < _inFlightJobs.Count; i++)
        {
            if (_inFlightJobs[i].Job.Status != JobStatus.Finished)
                continue;

            finishedIndices ??= new List<int>();
            finishedIndices.Add(i);
        }

        if (finishedIndices == null)
            return;

        // Walk backwards so removing by index doesn't shift the indices still to come.
        for (var i = finishedIndices.Count - 1; i >= 0; i--)
        {
            var idx = finishedIndices[i];
            var (job, cts, grids) = _inFlightJobs[idx];
            _inFlightJobs.RemoveAt(idx);
            cts.Dispose();

            foreach (var g in grids)
            {
                _busyGrids.Remove(g);
            }

            ApplyJobResult(grids, job);
        }
    }

    /// <summary>
    /// Applies a finished column job's result grid by grid: reaps whatever tile lost all stability
    /// (destroying whatever's anchored to it first) and replaces each grid's stored
    /// <see cref="CEGridStabilityComponent.Stability"/>. No cross-grid cascade step is needed here —
    /// the job already resolved the whole column together, so this grid's neighbors don't need to be
    /// separately re-dirtied the way a per-grid computation would have required.
    /// </summary>
    private void ApplyJobResult(List<EntityUid> grids, StabilityJob job)
    {
        if (job.Exception != null)
        {
            Log.Error($"ZCollapse: stability job faulted: {job.Exception}");
            return;
        }

        var result = job.Result ?? new Dictionary<(EntityUid, Vector2i), int>();

        var stabilityByGrid = new Dictionary<EntityUid, Dictionary<Vector2i, int>>();
        foreach (var ((nodeGrid, tile), value) in result)
        {
            if (!stabilityByGrid.TryGetValue(nodeGrid, out var dict))
                stabilityByGrid[nodeGrid] = dict = new Dictionary<Vector2i, int>();

            dict[tile] = value;
        }

        var liveByGrid = new Dictionary<EntityUid, HashSet<Vector2i>>();
        foreach (var (nodeGrid, tile) in job.LiveNodes)
        {
            if (!liveByGrid.TryGetValue(nodeGrid, out var set))
                liveByGrid[nodeGrid] = set = new HashSet<Vector2i>();

            set.Add(tile);
        }

        foreach (var gridUid in grids)
        {
            // The job can take several ticks; a grid may be gone by the time it's done (deleted,
            // Z-network torn down). That's an expected race, not a bug — just skip it.
            if (!_stabilityQuery.TryGetComponent(gridUid, out var comp) || !_gridQuery.TryGetComponent(gridUid, out var grid))
                continue;

            var newStability = stabilityByGrid.GetValueOrDefault(gridUid) ?? new Dictionary<Vector2i, int>();
            var liveTiles = liveByGrid.GetValueOrDefault(gridUid) ?? new HashSet<Vector2i>();

            ScheduleCollapsingTiles(gridUid, grid, comp, liveTiles, newStability);

            comp.Stability.Clear();
            foreach (var (tile, value) in newStability)
            {
                comp.Stability[tile] = value;
            }

            _debugDirtyGrids.Add(gridUid);
        }
    }

    /// <summary>
    /// For every tile that was live when the job snapshot was taken but has no stability in the job's
    /// result, spawns the first collapse dust puff and starts its random 2-4s countdown (unless one's
    /// already running for it); for every tile that regained stability since, cancels any countdown in
    /// progress. Skips tiles already gone, indestructible tiles, and anything in mapping mode (map
    /// editors previewing a broken layout shouldn't have tiles start crumbling). The tile itself only
    /// actually changes once the countdown fires — see <see cref="CollapseTile"/>.
    /// </summary>
    private void ScheduleCollapsingTiles(EntityUid gridUid, MapGridComponent grid, CEGridStabilityComponent comp, IReadOnlySet<Vector2i> liveTiles, Dictionary<Vector2i, int> newStability)
    {
        if (!_map.IsInitialized(Transform(gridUid).MapUid))
            return;

        foreach (var tile in liveTiles)
        {
            if (newStability.ContainsKey(tile))
            {
                comp.PendingCollapses.Remove(tile); // shored back up before it fell — cancel
                continue;
            }

            if (comp.PendingCollapses.ContainsKey(tile))
                continue; // already crumbling — don't restart its clock or double the dust puff

            if (!_map.TryGetTile(grid, tile, out var currentTile) || currentTile.IsEmpty)
                continue;

            if (_tileDefMan[currentTile.TypeId] is ContentTileDefinition { Indestructible: true })
                continue;

            var coords = _map.GridTileToLocal(gridUid, grid, tile);
            SpawnAtPosition(CollapseDustEffect, coords);
            _audio.PlayPvs(CollapseSound, coords);
            comp.PendingCollapses[tile] = _timing.CurTime + TimeSpan.FromSeconds(_random.NextFloat(CollapseDelayMinSeconds, CollapseDelayMaxSeconds));
        }
    }

    /// <summary>Fires every grid's due collapse countdowns. A plain per-tick scan over participating grids — collapses in flight at once are rare and few, so this stays cheap without needing its own dirty-tracking.</summary>
    private void ProcessPendingCollapses()
    {
        var query = AllEntityQuery<CEGridStabilityComponent, MapGridComponent>();
        while (query.MoveNext(out var gridUid, out var comp, out var grid))
        {
            if (comp.PendingCollapses.Count == 0)
                continue;

            List<Vector2i>? due = null;
            foreach (var (tile, collapseAt) in comp.PendingCollapses)
            {
                if (collapseAt > _timing.CurTime)
                    continue;

                due ??= new List<Vector2i>();
                due.Add(tile);
            }

            if (due == null)
                continue;

            foreach (var tile in due)
            {
                comp.PendingCollapses.Remove(tile);
                CollapseTile(gridUid, grid, tile);
            }
        }
    }

    /// <summary>
    /// The delayed second half of a tile's collapse: a second dust puff, destroying whatever's
    /// anchored to it, dropping the tile's normal deconstruction item onto the level below (see
    /// <see cref="DropTileItemBelow"/>), and stepping the tile down to its BaseTurf layer instead of
    /// straight to empty space — a tile with further layers below it (e.g. floor -&gt; plating -&gt;
    /// lattice) just gets thinner, and only disappears once ZCollapse finds THAT layer unsupported too
    /// on some later recompute (the tile change here fires <c>TileChangedEvent</c>, which already
    /// marks the grid dirty either way). That's what makes the collapse read as gradual/recursive
    /// rather than a single tile-sized hole appearing instantly.
    /// </summary>
    private void CollapseTile(EntityUid gridUid, MapGridComponent grid, Vector2i tile)
    {
        if (!_map.TryGetTile(grid, tile, out var currentTile) || currentTile.IsEmpty)
            return; // something else (RCD, explosion) already took it — nothing left to collapse

        var tileDef = (ContentTileDefinition)_tileDefMan[currentTile.TypeId];
        if (tileDef.Indestructible)
            return;

        var collapseCoords = _map.GridTileToLocal(gridUid, grid, tile);
        SpawnAtPosition(CollapseDustEffect, collapseCoords);
        _audio.PlayPvs(CollapseSound, collapseCoords);

        DestroyAnchoredEntities(gridUid, grid, tile);
        DropTileItemBelow(gridUid, tile, tileDef);

        var nextTile = tileDef.BaseTurf is { } baseTurf ? new Tile(_tileDefMan[baseTurf].TileId) : Tile.Empty;
        _map.SetTile(gridUid, grid, tile, nextTile);
    }

    /// <summary>
    /// Spawns the tile's normal deconstruction item on the grid directly below, at the same tile
    /// position, with <see cref="CEZPhysicsComponent.LocalPosition"/> set near the ceiling so it reads
    /// as having just fallen through the hole this collapse opened rather than appearing mid-floor —
    /// the existing Z-physics gravity/fall handling carries it down from there on its own. Z-adjacent
    /// participating grids are tile-aligned by construction (the same assumption Support bridging
    /// already relies on), so the tile index is reused directly with no world-position lookup needed.
    /// </summary>
    private void DropTileItemBelow(EntityUid gridUid, Vector2i tile, ContentTileDefinition tileDef)
    {
        var itemProto = tileDef.ItemDropPrototypeName;
        if (itemProto == null)
            return;

        if (!_zMapQuery.TryGetComponent(gridUid, out var zMap) ||
            !_zLevel.TryMapDown((gridUid, zMap), out var below) ||
            !_gridQuery.TryGetComponent(below.Owner, out var belowGrid))
        {
            return; // nothing below to fall onto (e.g. bottom of the station) — the item is simply lost
        }

        var item = Spawn(itemProto, _map.GridTileToLocal(below.Owner, belowGrid, tile));
        _zLevel.SetZPosition(item, 0.9f);

        Transform(item).LocalRotation = _random.NextAngle();
        if (TryComp<PhysicsComponent>(item, out var physics))
            _physics.ApplyLinearImpulse(item, _random.NextVector2(DropImpulseMin, DropImpulseMax), body: physics);
    }

    /// <summary>
    /// Force-destroys everything anchored to a collapsing tile. Entities with a
    /// <see cref="DamageableComponent"/> get dealt overwhelming Structural/Blunt damage instead of
    /// being deleted outright, so their own Destructible thresholds run normally (rubble, material
    /// refund, break sound — same pipeline explosions and shuttle impacts use); anything else falls
    /// back to <see cref="SharedDestructibleSystem.DestroyEntity"/> for whatever cleanup it defines
    /// off <c>DestructionEventArgs</c> (e.g. spilling a locker's contents).
    /// </summary>
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
            if (_damageableQuery.TryGetComponent(uid, out var damageable))
                _damageable.TryChangeDamage((uid, damageable), CollapseDamage, ignoreResistances: true, ignoreGlobalModifiers: true);
            _destructible.DestroyEntity(uid);
        }
    }
}
