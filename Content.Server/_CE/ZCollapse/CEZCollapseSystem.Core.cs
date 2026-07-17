namespace Content.Server._CE.ZCollapse;

public sealed partial class CEZCollapseSystem
{
    private void InitializeCore()
    {
        SubscribeLocalEvent<CEGridStabilityCoreComponent, AnchorStateChangedEvent>(OnCoreAnchorChanged);
        SubscribeLocalEvent<CEGridStabilityCoreComponent, ReAnchorEvent>(OnCoreReAnchor);

        // Entities that start already-anchored from map/prototype data don't go through
        // AnchorEntity() (and so never raise AnchorStateChangedEvent). No MapInitEvent catch-up
        // needed here though: CEZCollapseSystem.OnStabilityMapInit runs one full ForceRecalculateGrid
        // per grid after map init finishes, which scans for every anchored Core directly.
    }

    private void OnCoreAnchorChanged(Entity<CEGridStabilityCoreComponent> ent, ref AnchorStateChangedEvent args)
    {
        var xform = args.Transform;
        if (xform.GridUid is not { } gridUid ||
            !_gridQuery.TryGetComponent(gridUid, out var grid) ||
            !_stabilityQuery.TryGetComponent(gridUid, out var comp))
        {
            return;
        }

        var tile = _map.TileIndicesFor(gridUid, grid, xform.Coordinates);
        SetCoreSeed(gridUid, comp, tile, args.Anchored ? ent.Comp.LevitationForce : null);
    }

    private void OnCoreReAnchor(Entity<CEGridStabilityCoreComponent> ent, ref ReAnchorEvent args)
    {
        if (_stabilityQuery.TryGetComponent(args.OldGrid, out var oldComp))
            SetCoreSeed(args.OldGrid, oldComp, args.TilePos, null);

        if (_stabilityQuery.TryGetComponent(args.Grid, out var newComp))
            SetCoreSeed(args.Grid, newComp, args.TilePos, ent.Comp.LevitationForce);
    }
}
