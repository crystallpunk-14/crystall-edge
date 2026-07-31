using Content.Shared._CE.InfusionAltar.Components;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Server._CE.InfusionAltar;

public sealed partial class CEInfusionAltarSystem
{
    [Dependency] private SharedMapSystem _mapSystem = default!;
    [Dependency] private EntityQuery<MapGridComponent> _mapGridQuery = default!;

    private void InitConnections()
    {
        SubscribeLocalEvent<CEInfusionAltarComponent, AnchorStateChangedEvent>(OnAltarAnchorChanged);
        SubscribeLocalEvent<CEInfusionAltarSubPedestalComponent, AnchorStateChangedEvent>(OnSubPedestalAnchorChanged);
        SubscribeLocalEvent<CEInfusionAltarSubPedestalComponent, EntityTerminatingEvent>(OnSubPedestalTerminating);
        SubscribeLocalEvent<CEInfusionAltarSubPedestalComponent, ComponentRemove>(OnSubPedestalRemoved);
    }

    private void OnAltarAnchorChanged(Entity<CEInfusionAltarComponent> ent, ref AnchorStateChangedEvent args)
    {
        if (!args.Anchored)
        {
            ent.Comp.ConnectedPedestals.Clear();
            return;
        }

        RescanPedestals(ent);
    }

    /// <summary>
    /// Rebuilds this altar's connected pedestal set from scratch by checking every possible position
    /// for a currently-anchored sub-pedestal.
    /// </summary>
    private void RescanPedestals(Entity<CEInfusionAltarComponent> altar)
    {
        altar.Comp.ConnectedPedestals.Clear();

        if (!TryGetTile(altar.Owner, out var altarGrid, out var altarTile))
            return;

        var query = EntityQueryEnumerator<CEInfusionAltarSubPedestalComponent, TransformComponent>();
        while (query.MoveNext(out var pedestalUid, out _, out var pedestalXform))
        {
            if (!pedestalXform.Anchored || pedestalXform.GridUid != altarGrid)
                continue;

            var pedestalTile = _mapSystem.TileIndicesFor(altarGrid, _mapGridQuery.Comp(altarGrid), pedestalXform.Coordinates);
            if (altar.Comp.PossiblePedestalsPositions.Contains(pedestalTile - altarTile))
                altar.Comp.ConnectedPedestals.Add(pedestalUid);
        }
    }

    private void OnSubPedestalAnchorChanged(Entity<CEInfusionAltarSubPedestalComponent> ent, ref AnchorStateChangedEvent args)
    {
        if (!TryGetTile(ent.Owner, out var pedestalGrid, out var pedestalTile))
            return;

        var query = EntityQueryEnumerator<CEInfusionAltarComponent>();
        while (query.MoveNext(out var altarUid, out var altarComp))
        {
            if (!TryGetTile(altarUid, out var altarGrid, out var altarTile) || altarGrid != pedestalGrid)
                continue;

            if (!altarComp.PossiblePedestalsPositions.Contains(pedestalTile - altarTile))
                continue;

            if (args.Anchored)
                altarComp.ConnectedPedestals.Add(ent.Owner);
            else
                altarComp.ConnectedPedestals.Remove(ent.Owner);
        }
    }

    private void OnSubPedestalTerminating(Entity<CEInfusionAltarSubPedestalComponent> ent, ref EntityTerminatingEvent args)
    {
        PruneFromAllAltars(ent.Owner);
    }

    private void OnSubPedestalRemoved(Entity<CEInfusionAltarSubPedestalComponent> ent, ref ComponentRemove args)
    {
        PruneFromAllAltars(ent.Owner);
    }

    private void PruneFromAllAltars(EntityUid pedestal)
    {
        var query = EntityQueryEnumerator<CEInfusionAltarComponent>();
        while (query.MoveNext(out _, out var altarComp))
        {
            altarComp.ConnectedPedestals.Remove(pedestal);
        }
    }

    /// <summary>
    /// Resolves the grid and tile indices an entity currently occupies, regardless of its anchor state.
    /// </summary>
    private bool TryGetTile(EntityUid uid, out EntityUid grid, out Vector2i tile)
    {
        grid = default;
        tile = default;

        var xform = Transform(uid);
        if (xform.GridUid is not { } gridUid || !_mapGridQuery.TryComp(gridUid, out var gridComp))
            return false;

        grid = gridUid;
        tile = _mapSystem.TileIndicesFor(gridUid, gridComp, xform.Coordinates);
        return true;
    }
}
