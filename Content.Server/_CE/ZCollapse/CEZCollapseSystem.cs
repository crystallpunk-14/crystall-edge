using Content.Shared._CE.ZLevels.Core.Components;
using Content.Shared._CE.ZLevels.Core.EntitySystems;
using Content.Shared.Maps;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Server._CE.ZCollapse;

/// <summary>
/// Computes and enforces per-tile structural stability for floating grids: a
/// <see cref="CEGridStabilityCoreComponent"/> seeds its tile, stability flood-fills outward tile by tile
/// losing 1 per hop, and <see cref="CEGridStabilitySupportComponent"/> bridges that flood between a grid and
/// the Z-level directly above it. Any tile whose stability reaches 0 is deleted.
///
/// Only grids carrying <see cref="CEGridStabilityComponent"/> participate (opt-in, see that
/// component's docs). See docs/superpowers/specs (plan file) for the full design.
/// </summary>
public sealed partial class CEZCollapseSystem : EntitySystem
{
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private CESharedZLevelsSystem _zLevel = default!;
    [Dependency] private ITileDefinitionManager _tileDefMan = default!;

    [Dependency] private EntityQuery<CEGridStabilityComponent> _stabilityQuery = default!;
    [Dependency] private EntityQuery<CEGridStabilityCoreComponent> _coreQuery = default!;
    [Dependency] private EntityQuery<CEGridStabilitySupportComponent> _supportQuery = default!;
    [Dependency] private EntityQuery<MapGridComponent> _gridQuery = default!;
    [Dependency] private EntityQuery<CEZMapComponent> _zMapQuery = default!;

    private static readonly Vector2i[] CardinalOffsets =
    {
        new(1, 0), new(-1, 0), new(0, 1), new(0, -1),
    };

    /// <summary>
    /// (grid, tile) pairs whose Support bridge needs re-evaluating. Deferred to next Update() rather
    /// than processed inline, so a tall Z-stack cascade resolves over N ticks instead of recursing
    /// unbounded within a single tick.
    /// </summary>
    private HashSet<(EntityUid Grid, Vector2i Tile)> _pendingBridgeChecks = new();

    /// <summary>
    /// Grids awaiting their one-time post-map-init sweep (see <see cref="OnStabilityMapInit"/>).
    /// </summary>
    private HashSet<EntityUid> _pendingMapInitReap = new();

    /// <summary>Grids whose stability data changed since the last debug-overlay push.</summary>
    private readonly HashSet<EntityUid> _debugDirtyGrids = new();

    private const int MaxBridgeSettleIterations = 64;

    public override void Initialize()
    {
        base.Initialize();

        InitializeCore();
        InitializeSupport();
        InitializeTileEvents();
        InitializeDebug();

        SubscribeLocalEvent<CEGridStabilityComponent, MapInitEvent>(OnStabilityMapInit);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // Seed every grid that just finished MapInit from its own Cores only (order-independent — it
        // never reads another grid's state) and queue its Supports into the normal bridge settle loop
        // below, rather than resolving bridges here directly. A floor with no Core of its own, fed
        // only by a Support bridge from another floor, depends on that other floor's Core having
        // already flooded — which grid finishes first is unspecified, so only the settle loop's
        // multi-iteration convergence (unchanged, same as any live anchor/unanchor cascade) handles
        // that correctly. Reaping is deferred past the settle loop for the same reason: judging a
        // bridge-fed floor dead before its bridge has been evaluated at all would wrongly nuke it.
        HashSet<EntityUid>? justInited = null;
        if (_pendingMapInitReap.Count > 0)
        {
            justInited = _pendingMapInitReap;
            _pendingMapInitReap = new HashSet<EntityUid>();

            foreach (var gridUid in justInited)
            {
                SeedMapInitGrid(gridUid);
            }
        }

        DrainBridgeChecks();

        if (justInited != null)
        {
            foreach (var gridUid in justInited)
            {
                ReapOrphanTiles(gridUid);
            }
        }

        PushDirtySnapshots();
    }

    /// <summary>
    /// Drains <see cref="_pendingBridgeChecks"/> to a fixed point within this single tick (bounded)
    /// rather than one wave per tick — a tall Z-stack would otherwise visibly "settle" over many
    /// frames, which reads as flickering. The iteration cap is a safety net, not the expected case: if
    /// it's ever hit, that's a real non-convergence bug, not normal cascade depth.
    /// </summary>
    private void DrainBridgeChecks()
    {
        var iterations = 0;
        while (_pendingBridgeChecks.Count > 0 && iterations++ < MaxBridgeSettleIterations)
        {
            var toProcess = _pendingBridgeChecks;
            _pendingBridgeChecks = new HashSet<(EntityUid, Vector2i)>();

            foreach (var (gridUid, tile) in toProcess)
            {
                RecomputeBridge(gridUid, tile);
            }
        }

        if (_pendingBridgeChecks.Count > 0)
        {
            Log.Warning($"ZCollapse: bridge recheck queue did not settle after {MaxBridgeSettleIterations} iterations ({_pendingBridgeChecks.Count} pending) — likely a non-convergent seed cycle.");
            _pendingBridgeChecks.Clear();
        }
    }

    /// <summary>
    /// A freshly-loaded grid's Core/Support entities never went through AnchorEntity() (they start
    /// already-anchored from map/prototype data), so nothing seeded this grid incrementally as it
    /// loaded — it needs one cold-start recompute. Deferred to next Update() rather than run inline:
    /// MapInitEvent fires for this grid entity *before* its child Core/Support entities get their own
    /// MapInitEvent (breadth-first order), so recomputing here immediately would see no entities yet;
    /// by next tick, map init has fully finished (it runs to completion before the game loop starts
    /// ticking), so <see cref="SeedMapInitGrid"/>'s own entity scan finds all of them at once.
    ///
    /// This also means Core/Support don't need their own MapInitEvent catch-up handlers — one seeding
    /// pass per grid here replaces what would otherwise be N separate live flood-fill passes (one per
    /// entity) during load, which is both redundant and, for grids with hundreds of structural
    /// entities, the dominant cost of a slow map load.
    /// </summary>
    private void OnStabilityMapInit(Entity<CEGridStabilityComponent> ent, ref MapInitEvent args)
    {
        _pendingMapInitReap.Add(ent);
    }

    /// <summary>
    /// Cold-start seeding for a grid that just finished MapInit: registers this grid's own Cores and
    /// does one combined Propagate from all of them (safe and order-independent — never reads another
    /// grid's state), then queues every Support anchored here into <see cref="_pendingBridgeChecks"/>
    /// instead of resolving bridges synchronously. Deliberately does not reap — see Update()'s
    /// map-init block for why that has to wait until the bridge settle loop converges.
    /// </summary>
    private void SeedMapInitGrid(EntityUid gridUid)
    {
        if (!_stabilityQuery.TryGetComponent(gridUid, out var comp) || !_gridQuery.TryGetComponent(gridUid, out var grid))
            return;

        var queue = new Queue<(Vector2i, int)>();
        var coreQuery = AllEntityQuery<CEGridStabilityCoreComponent, TransformComponent>();
        while (coreQuery.MoveNext(out _, out var core, out var xform))
        {
            if (xform.GridUid != gridUid || !xform.Anchored)
                continue;

            var tile = _map.TileIndicesFor(gridUid, grid, xform.Coordinates);
            var value = Math.Max(comp.CoreSeeds.GetValueOrDefault(tile, 0), core.LevitationForce);
            comp.CoreSeeds[tile] = value;
            comp.Seeds[tile] = Math.Max(comp.Seeds.GetValueOrDefault(tile, 0), value);
            queue.Enqueue((tile, value));
        }

        if (queue.Count > 0)
            Propagate(grid, comp, queue);

        var supportQuery = AllEntityQuery<CEGridStabilitySupportComponent, TransformComponent>();
        while (supportQuery.MoveNext(out _, out _, out var xform))
        {
            if (xform.GridUid != gridUid || !xform.Anchored)
                continue;

            _pendingBridgeChecks.Add((gridUid, _map.TileIndicesFor(gridUid, grid, xform.Coordinates)));
        }

        _debugDirtyGrids.Add(gridUid);
    }

    /// <summary>
    /// Catches tiles that Propagate and the bridge settle loop structurally can't reach at all — a
    /// platform with no path to any Core or Support bridge never enters either BFS, so it never gets a
    /// <see cref="CEGridStabilityComponent.Stability"/> entry and is invisible to the normal
    /// touched-tile reap in <see cref="FinishRecompute"/>.
    /// </summary>
    private void ReapOrphanTiles(EntityUid gridUid)
    {
        if (!_stabilityQuery.TryGetComponent(gridUid, out var comp) || !_gridQuery.TryGetComponent(gridUid, out var grid))
            return;

        var orphans = new HashSet<Vector2i>();
        var enumerator = _map.GetAllTilesEnumerator(gridUid, grid);
        while (enumerator.MoveNext(out var tileRef))
        {
            if (!comp.Stability.ContainsKey(tileRef.Value.GridIndices))
                orphans.Add(tileRef.Value.GridIndices);
        }

        ReapDeadTiles(gridUid, grid, comp, orphans);
    }

    private static IEnumerable<Vector2i> GetFourNeighbors(Vector2i tile)
    {
        foreach (var offset in CardinalOffsets)
        {
            yield return tile + offset;
        }
    }

    private bool IsTileAlive(MapGridComponent grid, Vector2i tile)
    {
        return _map.TryGetTile(grid, tile, out var t) && !t.IsEmpty;
    }

    private void ReapDeadTiles(EntityUid gridUid, MapGridComponent grid, CEGridStabilityComponent comp, IEnumerable<Vector2i> touched)
    {
        if (!_map.IsInitialized(Transform(gridUid).MapUid))
            return; // mapping mode: preview only, never delete — see OnStabilityMapInit for the catch-up pass

        List<(Vector2i, Tile)>? toDelete = null;
        foreach (var tile in touched)
        {
            if (comp.Stability.ContainsKey(tile))
                continue; // still alive

            if (!_map.TryGetTile(grid, tile, out var currentTile) || currentTile.IsEmpty)
                continue; // already empty

            if (_tileDefMan[currentTile.TypeId] is ContentTileDefinition { Indestructible: true })
                continue;

            DestroyAnchoredEntities(gridUid, grid, tile);

            toDelete ??= new List<(Vector2i, Tile)>();
            toDelete.Add((tile, Tile.Empty));
        }

        if (toDelete is { Count: > 0 })
            _map.SetTiles(gridUid, grid, toDelete);
    }

    /// <summary>
    /// Marks a grid dirty for the debug overlay and queues Support-bridge re-evaluation for tiles
    /// whose alive-state may have changed. Call after any Propagate/Depropagate pass.
    /// </summary>
    private void FinishRecompute(EntityUid gridUid, MapGridComponent grid, CEGridStabilityComponent comp, HashSet<Vector2i> touched)
    {
        if (touched.Count == 0)
            return;

        ReapDeadTiles(gridUid, grid, comp, touched);
        _debugDirtyGrids.Add(gridUid);

        foreach (var tile in touched)
        {
            QueueBridgeRecheck(gridUid, tile);
        }
    }

    /// <summary>
    /// A stability change at (gridUid, tile) can affect: a Support standing there (bridges up to the
    /// level above), and a Support standing at the same tile one level below (bridges up into us).
    /// </summary>
    private void QueueBridgeRecheck(EntityUid gridUid, Vector2i tile)
    {
        _pendingBridgeChecks.Add((gridUid, tile));

        if (_zMapQuery.TryGetComponent(gridUid, out var zMap) && _zLevel.TryMapDown((gridUid, zMap), out var belowMap))
            _pendingBridgeChecks.Add((belowMap.Owner, tile));
    }

    private void RunPropagate(EntityUid gridUid, MapGridComponent grid, CEGridStabilityComponent comp, Queue<(Vector2i Tile, int Value)> queue)
    {
        var touched = Propagate(grid, comp, queue);
        FinishRecompute(gridUid, grid, comp, touched);
    }

    private void RunDepropagate(EntityUid gridUid, MapGridComponent grid, CEGridStabilityComponent comp, Queue<(Vector2i Tile, int Value)> popQueue)
    {
        var touched = Depropagate(grid, comp, popQueue);
        FinishRecompute(gridUid, grid, comp, touched);
    }

    private void SetCoreSeed(EntityUid gridUid, CEGridStabilityComponent comp, Vector2i tile, int? value)
    {
        UpdateSeedSource(gridUid, comp, comp.CoreSeeds, tile, value);
    }

    private void SetBridgeSeedFromAbove(EntityUid gridUid, CEGridStabilityComponent comp, Vector2i tile, int? value)
    {
        UpdateSeedSource(gridUid, comp, comp.BridgeSeedsFromAbove, tile, value);
    }

    private void SetBridgeSeedFromBelow(EntityUid gridUid, CEGridStabilityComponent comp, Vector2i tile, int? value)
    {
        UpdateSeedSource(gridUid, comp, comp.BridgeSeedsFromBelow, tile, value);
    }

    /// <summary>
    /// Core and Support seeds are tracked in separate dictionaries so removing one source doesn't
    /// blow away an independent seed from another; <see cref="CEGridStabilityComponent.Seeds"/>
    /// always holds the max of all three, which is what Propagate/Depropagate actually consume.
    /// </summary>
    private void UpdateSeedSource(EntityUid gridUid, CEGridStabilityComponent comp, Dictionary<Vector2i, int> source, Vector2i tile, int? value)
    {
        if (value is { } v and > 0)
            source[tile] = v;
        else
            source.Remove(tile);

        var newCombined = Math.Max(comp.CoreSeeds.GetValueOrDefault(tile, 0),
            Math.Max(comp.BridgeSeedsFromAbove.GetValueOrDefault(tile, 0), comp.BridgeSeedsFromBelow.GetValueOrDefault(tile, 0)));
        var oldCombined = comp.Seeds.GetValueOrDefault(tile, 0);
        if (newCombined == oldCombined)
            return;

        if (newCombined > 0)
            comp.Seeds[tile] = newCombined;
        else
            comp.Seeds.Remove(tile);

        if (!_gridQuery.TryGetComponent(gridUid, out var grid))
            return;

        if (newCombined > oldCombined)
        {
            var queue = new Queue<(Vector2i, int)>();
            queue.Enqueue((tile, newCombined));
            RunPropagate(gridUid, grid, comp, queue);
        }
        else
        {
            var queue = new Queue<(Vector2i, int)>();
            queue.Enqueue((tile, oldCombined));
            RunDepropagate(gridUid, grid, comp, queue);
        }
    }
}
