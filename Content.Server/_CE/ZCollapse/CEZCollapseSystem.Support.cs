using Robust.Shared.Map;

namespace Content.Server._CE.ZCollapse;

public sealed partial class CEZCollapseSystem
{
    private void InitializeSupport()
    {
        SubscribeLocalEvent<CEGridStabilitySupportComponent, AnchorStateChangedEvent>(OnSupportAnchorChanged);
        SubscribeLocalEvent<CEGridStabilitySupportComponent, ReAnchorEvent>(OnSupportReAnchor);

        // Entities that start already-anchored from map/prototype data don't go through
        // AnchorEntity() (and so never raise AnchorStateChangedEvent). No MapInitEvent catch-up
        // needed here though: CEZCollapseSystem.OnStabilityMapInit runs one full ForceRecalculateGrid
        // per grid after map init finishes, which scans for every anchored Support directly.
    }

    private void OnSupportAnchorChanged(Entity<CEGridStabilitySupportComponent> ent, ref AnchorStateChangedEvent args)
    {
        var xform = args.Transform;
        if (xform.GridUid is not { } gridUid || !_gridQuery.TryGetComponent(gridUid, out var grid))
            return;

        var tile = _map.TileIndicesFor(gridUid, grid, xform.Coordinates);
        RecomputeBridge(gridUid, tile);
    }

    private void OnSupportReAnchor(Entity<CEGridStabilitySupportComponent> ent, ref ReAnchorEvent args)
    {
        RecomputeBridge(args.OldGrid, args.TilePos);
        RecomputeBridge(args.Grid, args.TilePos);
    }

    /// <summary>
    /// Recomputes the Support bridge at (gridUid, tile) from scratch, based on whichever
    /// <see cref="CEGridStabilitySupportComponent"/> entities are currently anchored there (taking the
    /// strongest if more than one). This is always a full re-derivation from current ground truth
    /// rather than an incremental add/remove, so it correctly handles anchor, unanchor, re-anchor,
    /// and multiple stacked supports uniformly with no extra bookkeeping.
    ///
    /// A Support is a capped pass-through, not a source: it transfers at most its own
    /// <see cref="CEGridStabilitySupportComponent.SupportStrength"/>, but never more than the
    /// donating side actually has. Seeding a flat SupportStrength any time the far side was merely
    /// "alive" (regardless of how little stability it actually had) was manufacturing stability out
    /// of nothing, and let two floors with no real Core anywhere sustain each other indefinitely.
    /// </summary>
    private void RecomputeBridge(EntityUid gridUid, Vector2i tile)
    {
        if (!_stabilityQuery.TryGetComponent(gridUid, out var comp) || !_gridQuery.TryGetComponent(gridUid, out var grid))
            return;

        var strength = 0;
        if (IsTileAlive(grid, tile))
        {
            var enumerator = _map.GetAnchoredEntitiesEnumerator(gridUid, grid, tile);
            while (enumerator.MoveNext(out var anchored))
            {
                if (_supportQuery.TryGetComponent(anchored.Value, out var support))
                    strength = Math.Max(strength, support.SupportStrength);
            }
        }

        Entity<CEGridStabilityComponent>? above = null;
        if (_zMapQuery.TryGetComponent(gridUid, out var zMap) &&
            _zLevel.TryMapUp((gridUid, zMap), out var aboveMap) &&
            _stabilityQuery.TryGetComponent(aboveMap.Owner, out var aboveComp))
        {
            above = (aboveMap.Owner, aboveComp);
        }

        var aboveValue = above is { } aboveCheck ? aboveCheck.Comp.Stability.GetValueOrDefault(tile, 0) : 0;

        // Feed down: current grid gets at most min(strength, what's actually above). Only this call
        // (for this exact gridUid/tile) is ever allowed to touch BridgeSeedsFromAbove here — the
        // paired grid above's own RecomputeBridge call never writes it, so there's no other writer to
        // race against.
        SetBridgeSeedFromAbove(gridUid, comp, tile, strength > 0 && aboveValue > 0 ? Math.Min(strength, aboveValue) : null);

        // Feed up: the grid above gets at most min(strength, what's actually here). Re-read
        // Stability now rather than reuse a value captured before the feed-down write above, since
        // that write can synchronously change this tile's own Stability (via SetBridgeSeedFromAbove's
        // Propagate/Depropagate) before this line runs.
        if (above is { } aboveEnt)
        {
            var currentValue = comp.Stability.GetValueOrDefault(tile, 0);
            SetBridgeSeedFromBelow(aboveEnt.Owner, aboveEnt.Comp, tile, strength > 0 && currentValue > 0 ? Math.Min(strength, currentValue) : null);
        }
    }
}
