using Content.Client._CE.Wallpaper;
using Content.Client.IconSmoothing;
using Content.Shared._CE.WallOverlap;
using Content.Shared._CE.Wallpaper;
using Robust.Client.GameObjects;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Utility;
using static Robust.Client.GameObjects.SpriteComponent;

namespace Content.Client._CE.WallOverlap;

/// <summary>
/// Draws the sprite of a CEWallOverlapComponent wall on the walls next to it, as an extra layer on the neighbour's own
/// sprite. Purely derived from the walls around, nothing is saved or networked, so breaking the source wall
/// removes the overlap by itself. The layers sit above the IconSmooth layers but below wallpaper.
/// </summary>
public sealed partial class CEClientWallOverlapSystem : EntitySystem
{
    [Dependency] private SpriteSystem _sprite = default!;
    [Dependency] private SharedMapSystem _map = default!;

    [Dependency] private EntityQuery<CEWallOverlapComponent> _sourceQuery = default!;
    [Dependency] private EntityQuery<IconSmoothComponent> _smoothQuery = default!;
    [Dependency] private EntityQuery<SpriteComponent> _spriteQuery = default!;
    [Dependency] private EntityQuery<MapGridComponent> _gridQuery = default!;

    [SubscribeLocalEvent]
    private void OnSourceStartup(Entity<CEWallOverlapComponent> ent, ref ComponentStartup args) => RefreshNeighbours(ent, null);

    [SubscribeLocalEvent]
    private void OnSourceShutdown(Entity<CEWallOverlapComponent> ent, ref ComponentShutdown args) => RefreshNeighbours(ent, ent.Owner);

    // Broadcast subscription: IconSmoothSystem already owns the IconSmoothComponent + AnchorStateChangedEvent and
    // ComponentStartup pairs, and the engine throws on duplicates. The event is also raised when an anchored wall
    // spawns, so this covers walls appearing, being built and being unanchored.
    [SubscribeLocalEvent]
    private void OnAnchorChanged(ref AnchorStateChangedEvent args)
    {
        var uid = args.Entity;

        if (_sourceQuery.HasComp(uid))
        {
            // A source being deleted is unanchored first while already terminating, so it must not be skipped here.
            // An unanchored source is no longer in the tile lookup, a re-anchored one is.
            RefreshNeighbours(uid, args.Anchored ? null : uid);
        }
        else if (!TerminatingOrDeleted(uid) && _smoothQuery.HasComp(uid))
        {
            Refresh(uid, null);
        }
    }

    /// <summary>
    /// Re-evaluates the overlap on the four walls around the source. <paramref name="ignored"/> is a source that is
    /// going away but can still be found in the tile lookup.
    /// </summary>
    private void RefreshNeighbours(EntityUid source, EntityUid? ignored)
    {
        if (!TryGetTile(source, out var grid, out var tile))
            return;

        foreach (var side in CESharedWallpaperSystem.Sides)
        {
            var enumerator = _map.GetAnchoredEntities(grid, tile.Offset(side));
            while (enumerator.MoveNext(out var neighbour))
            {
                if (_smoothQuery.HasComp(neighbour.Value))
                    Refresh(neighbour.Value, ignored);
            }
        }
    }

    private void Refresh(EntityUid target, EntityUid? ignored)
    {
        if (!_spriteQuery.TryComp(target, out var spriteComp))
            return;

        Entity<SpriteComponent?> sprite = (target, spriteComp);

        // RemoveLayer, not LayerMapRemove - the latter only drops the key and would leave the old layer drawn.
        foreach (var side in CESharedWallpaperSystem.Sides)
        {
            _sprite.RemoveLayer(sprite, LayerKey(side), logMissing: false);
        }

        // Sources never receive the overlap of other sources.
        if (_sourceQuery.HasComp(target)
            || !Transform(target).Anchored
            || !_smoothQuery.TryComp(target, out var smooth)
            || !TryGetTile(target, out var grid, out var tile))
            return;

        // Wallpaper layers are appended on top of everything, so the first one marks where the overlap has to go.
        var index = FirstWallpaperIndex(sprite);

        foreach (var side in CESharedWallpaperSystem.Sides)
        {
            if (!TryFindSource(grid, tile.Offset(side), smooth.SmoothKey, ignored, out var source))
                continue;

            var key = LayerKey(side);
            var layer = _sprite.AddLayer(sprite, new SpriteSpecifier.Rsi(source.Comp.Rsi, source.Comp.State), index);
            _sprite.LayerMapSet(sprite, key, layer);
            _sprite.LayerSetDirOffset(sprite, key, SideOffset(side));

            if (index != null)
                index++;
        }
    }

    private bool TryFindSource(
        Entity<MapGridComponent> grid,
        Vector2i tile,
        string targetKey,
        EntityUid? ignored,
        out Entity<CEWallOverlapComponent> source)
    {
        var enumerator = _map.GetAnchoredEntities(grid, tile);
        while (enumerator.MoveNext(out var ent))
        {
            if (ent.Value == ignored
                || TerminatingOrDeleted(ent.Value)
                || !_sourceQuery.TryComp(ent.Value, out var comp)
                || !comp.TargetKeys.Contains(targetKey))
                continue;

            source = (ent.Value, comp);
            return true;
        }

        source = default;
        return false;
    }

    private int? FirstWallpaperIndex(Entity<SpriteComponent?> sprite)
    {
        int? first = null;
        foreach (var side in CESharedWallpaperSystem.Sides)
        {
            if (_sprite.LayerMapTryGet(sprite, CEClientWallpaperSystem.LayerKey(side), out var index, false)
                && (first == null || index < first))
                first = index;
        }

        return first;
    }

    private bool TryGetTile(EntityUid uid, out Entity<MapGridComponent> grid, out Vector2i tile)
    {
        grid = default;
        tile = default;

        // Anchored is deliberately not required: a source being unanchored or deleted still has to find its tile
        // to clear the overlap off its neighbours.
        var xform = Transform(uid);
        if (xform.GridUid is not { } gridUid || !_gridQuery.TryComp(gridUid, out var gridComp))
            return false;

        grid = (gridUid, gridComp);
        tile = _map.TileIndicesFor(gridUid, gridComp, xform.Coordinates);
        return true;
    }

    private static string LayerKey(Direction side) => $"ce-wall-overlap-{side}";

    /// <summary>
    /// Same convention as wallpaper: the art is authored for the south side and every other side only relabels it
    /// through DirOffset, which also keeps it correct when the camera is rotated.
    /// </summary>
    private static DirectionOffset SideOffset(Direction side) => side switch
    {
        Direction.South => DirectionOffset.None,
        Direction.East => DirectionOffset.CounterClockwise,
        Direction.North => DirectionOffset.Flip,
        Direction.West => DirectionOffset.Clockwise,
        _ => DirectionOffset.None,
    };
}
