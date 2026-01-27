using Content.Shared._CE.Pipes;
using Content.Shared._CE.UnderWall;
using Robust.Client.GameObjects;
using Robust.Shared.Map.Components;

namespace Content.Client._CE.UnderWall;

public sealed class CEUnderWallSystem : EntitySystem
{
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CEUnderWallComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<CEUnderWallComponent, ComponentShutdown>(OnComponentShutdown);
        SubscribeLocalEvent<CEUnderWallComponent, AnchorStateChangedEvent>(OnAnchorStateChanged);

        // Subscribe to wall anchor changes, initialization and shutdown
        SubscribeLocalEvent<CEOccludePipesComponent, ComponentInit>(OnOccluderChanged);
        SubscribeLocalEvent<CEOccludePipesComponent, ComponentShutdown>(OnOccluderChanged);
        SubscribeLocalEvent<CEOccludePipesComponent, AnchorStateChangedEvent>(OnOccluderChanged);
    }

    private void OnOccluderChanged(Entity<CEOccludePipesComponent> ent, ref ComponentInit args)
    {
        UpdateOccluderTile(ent);
    }

    private void OnOccluderChanged(Entity<CEOccludePipesComponent> ent, ref ComponentShutdown args)
    {
        UpdateOccluderTile(ent);
    }

    private void OnOccluderChanged(Entity<CEOccludePipesComponent> ent, ref AnchorStateChangedEvent args)
    {
        UpdateOccluderTile(ent);
    }

    private void UpdateOccluderTile(Entity<CEOccludePipesComponent> ent)
    {
        var xform = Transform(ent);
        if (xform.GridUid == null || !TryComp<MapGridComponent>(xform.GridUid, out var grid))
            return;

        var tile = _map.TileIndicesFor(xform.GridUid.Value, grid, xform.Coordinates);

        // Update all CEUnderWall entities on the same tile
        UpdateEntitiesOnTile(xform.GridUid.Value, grid, tile);
    }

    private void OnComponentInit(Entity<CEUnderWallComponent> ent, ref ComponentInit args)
    {
        // Store the original drawDepth when component is added
        if (TryComp<SpriteComponent>(ent, out var sprite))
        {
            ent.Comp.OriginalDrawDepth = sprite.DrawDepth;
        }

        CheckAndUpdateWallStatus(ent);
    }

    private void OnComponentShutdown(Entity<CEUnderWallComponent> ent, ref ComponentShutdown args)
    {
        // Restore original drawDepth when component is removed
        if (ent.Comp.OriginalDrawDepth.HasValue && TryComp<SpriteComponent>(ent, out var sprite))
        {
            _sprite.SetDrawDepth((ent, sprite), ent.Comp.OriginalDrawDepth.Value);
        }
    }

    private void OnAnchorStateChanged(Entity<CEUnderWallComponent> ent, ref AnchorStateChangedEvent args)
    {
        CheckAndUpdateWallStatus(ent);
    }

    private void UpdateEntitiesOnTile(EntityUid gridUid, MapGridComponent grid, Vector2i tile)
    {
        var anchored = _map.GetAnchoredEntitiesEnumerator(gridUid, grid, tile);

        while (anchored.MoveNext(out var ent))
        {
            if (TryComp<CEUnderWallComponent>(ent, out var underWall))
            {
                CheckAndUpdateWallStatus((ent.Value, underWall));
            }
        }
    }

    private void CheckAndUpdateWallStatus(Entity<CEUnderWallComponent> ent)
    {
        if (!TryComp<SpriteComponent>(ent, out var sprite))
            return;

        var xform = Transform(ent);

        // Only process if anchored
        if (!xform.Anchored)
        {
            if (ent.Comp.IsBehindWall && ent.Comp.OriginalDrawDepth.HasValue)
            {
                ent.Comp.IsBehindWall = false;
                _sprite.SetDrawDepth((ent, sprite), ent.Comp.OriginalDrawDepth.Value);
            }
            return;
        }

        var gridUid = xform.GridUid;
        if (gridUid == null || !TryComp<MapGridComponent>(gridUid, out var grid))
        {
            if (ent.Comp.IsBehindWall && ent.Comp.OriginalDrawDepth.HasValue)
            {
                ent.Comp.IsBehindWall = false;
                _sprite.SetDrawDepth((ent, sprite), ent.Comp.OriginalDrawDepth.Value);
            }
            return;
        }

        var tile = _map.TileIndicesFor(gridUid.Value, grid, xform.Coordinates);
        var hasWall = HasWallOnTile(gridUid.Value, grid, tile, ent);

        if (hasWall && !ent.Comp.IsBehindWall)
        {
            ent.Comp.IsBehindWall = true;
            if (ent.Comp.OriginalDrawDepth.HasValue)
            {
                _sprite.SetDrawDepth((ent, sprite), ent.Comp.OriginalDrawDepth.Value - 1);
            }
        }
        else if (!hasWall && ent.Comp.IsBehindWall)
        {
            ent.Comp.IsBehindWall = false;
            if (ent.Comp.OriginalDrawDepth.HasValue)
            {
                _sprite.SetDrawDepth((ent, sprite), ent.Comp.OriginalDrawDepth.Value);
            }
        }
    }

    private bool HasWallOnTile(EntityUid gridUid, MapGridComponent grid, Vector2i tile, EntityUid ignore)
    {
        var anchored = _map.GetAnchoredEntitiesEnumerator(gridUid, grid, tile);

        while (anchored.MoveNext(out var ent))
        {
            if (ent == ignore)
                continue;

            if (HasComp<CEOccludePipesComponent>(ent))
                return true;
        }

        return false;
    }
}

