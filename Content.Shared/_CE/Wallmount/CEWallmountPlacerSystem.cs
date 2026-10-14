using Content.Shared._CE.ZLevels.Core.EntitySystems;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Maps;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Content.Shared.Stacks;
using Content.Shared.Tag;
using Content.Shared.Wall;
using Robust.Shared.Analyzers;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared._CE.Wallmount;

/// <summary>
/// Places wallmount entities from <see cref="CEWallmountPlacerComponent"/> items. The side of the wall is picked the
/// same way as wallpaper: the side facing the user along the larger axis, or the other facing side if that one is taken.
/// </summary>
public sealed partial class CEWallmountPlacerSystem : EntitySystem
{
    [Dependency] private INetManager _net = default!;
    [Dependency] private CESharedZLevelsSystem _zLevel = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedStackSystem _stack = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private TagSystem _tag = default!;
    [Dependency] private TurfSystem _turf = default!;

    [Dependency] private EntityQuery<CEWallmountComponent> _wallmountQuery = default!;
    [Dependency] private EntityQuery<CEWallmountedComponent> _wallmountedQuery = default!;
    [Dependency] private EntityQuery<MapGridComponent> _gridQuery = default!;
    [Dependency] private EntityQuery<WallComponent> _wallQuery = default!;

    private static readonly Direction[] CardinalDirections = { Direction.North, Direction.South, Direction.East, Direction.West };

    [SubscribeLocalEvent]
    private void OnAfterInteract(Entity<CEWallmountPlacerComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach)
            return;

        if (args.Target is { } target && IsWallSurface(target))
        {
            args.Handled = true;

            if (!TryGetWallSide(target, args.User, out var grid, out var tile, out var side))
            {
                _popup.PopupClient(Loc.GetString("ce-wallmount-placer-no-space"), target, args.User);
                return;
            }

            StartPlacing(ent, args.User, grid, tile, side, target);
            return;
        }

        if (!ent.Comp.AllowPlaceBelow)
            return;

        if (!TryGetBelowPlacement(args.User, args.ClickLocation, out var belowGrid, out var belowTile, out var belowSide, out var belowWall))
            return;

        args.Handled = true;
        StartPlacing(ent, args.User, belowGrid, belowTile, belowSide, belowWall);
    }

    [SubscribeLocalEvent]
    private void OnPlaceDoAfter(Entity<CEWallmountPlacerComponent> ent, ref CEWallmountPlaceDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        var gridUid = GetEntity(args.Grid);
        var wall = GetEntity(args.Wall);
        if (!_gridQuery.TryComp(gridUid, out var grid)
            || !IsWallSurface(wall)
            || !CanMountAt((gridUid, grid), args.Tile))
            return;

        args.Handled = true;

        var prototype = ent.Comp.Prototype;
        if (TryComp<StackComponent>(ent, out var stack) && stack.Count > 1)
            _stack.SetCount((ent.Owner, stack), stack.Count - 1);
        else
            PredictedQueueDel(ent.Owner);

        // Spawned on the server only: an anchored structure spawned in prediction would mispredict.
        if (_net.IsClient)
            return;

        SpawnAttachedTo(prototype, _map.GridTileToLocal(gridUid, grid, args.Tile), rotation: args.Side.ToAngle());
    }

    private void StartPlacing(Entity<CEWallmountPlacerComponent> ent, EntityUid user, EntityUid grid, Vector2i tile, Direction side, EntityUid wall)
    {
        var ev = new CEWallmountPlaceDoAfterEvent(GetNetEntity(grid), tile, side, GetNetEntity(wall));

        // No DoAfter target: when placing below, the wall is on another map and the range check would cancel it.
        var doAfterArgs = new DoAfterArgs(EntityManager, user, ent.Comp.Delay, ev, ent, used: ent)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            NeedHand = true,
        };

        _doAfter.TryStartDoAfter(doAfterArgs);
    }

    /// <summary>
    /// Free tile next to the wall on the side facing the user. Of the two facing sides, the one along the larger
    /// axis wins; the other is used only if the first is taken and the user is off-axis along it too.
    /// </summary>
    private bool TryGetWallSide(EntityUid wall, EntityUid user, out EntityUid gridUid, out Vector2i tile, out Direction side)
    {
        gridUid = default;
        tile = default;
        side = default;

        var xform = Transform(wall);
        if (!xform.Anchored || xform.GridUid is not { } grid || !_gridQuery.TryComp(grid, out var gridComp))
            return false;

        gridUid = grid;
        var wallTile = _map.TileIndicesFor(grid, gridComp, xform.Coordinates);
        var delta = _transform.GetWorldPosition(user) - _transform.GetWorldPosition(xform);
        var localDelta = (-_transform.GetWorldRotation(grid)).RotateVec(delta);

        var horizontal = localDelta.X > 0 ? Direction.East : Direction.West;
        var vertical = localDelta.Y > 0 ? Direction.North : Direction.South;
        var horizontalFirst = Math.Abs(localDelta.X) > Math.Abs(localDelta.Y);

        side = horizontalFirst ? horizontal : vertical;
        tile = wallTile + side.ToIntVec();
        if (CanMountAt((grid, gridComp), tile))
            return true;

        var fallbackOffset = horizontalFirst ? localDelta.Y : localDelta.X;
        if (Math.Abs(fallbackOffset) <= 0.01f)
            return false;

        side = horizontalFirst ? vertical : horizontal;
        tile = wallTile + side.ToIntVec();
        return CanMountAt((grid, gridComp), tile);
    }

    /// <summary>
    /// The user stands on a wall top and clicks the open tile next to it: the entity goes one level below, on that
    /// wall's side facing the clicked tile.
    /// </summary>
    private bool TryGetBelowPlacement(EntityUid user, EntityCoordinates clickLocation, out EntityUid gridUid, out Vector2i tile, out Direction side, out EntityUid wall)
    {
        gridUid = default;
        tile = default;
        side = default;
        wall = default;

        if (Transform(user).MapUid is not { } map || !_zLevel.TryMapDown((map, null), out var belowMap))
            return false;

        var clickPos = _transform.ToMapCoordinates(clickLocation).Position;
        if (_map.TryFindGridAt(map, clickPos, out var ownGrid, out var ownGridComp)
            && _map.TryGetTileRef(ownGrid, ownGridComp, clickPos, out var ownTile)
            && !ownTile.Tile.IsEmpty)
            return false;

        if (!_map.TryFindGridAt(belowMap.Owner, clickPos, out var grid, out var gridComp))
            return false;

        var wallTile = _map.WorldToTile(grid, gridComp, _transform.GetWorldPosition(user));
        var clickTile = _map.WorldToTile(grid, gridComp, clickPos);
        var offset = clickTile - wallTile;
        if (Math.Abs(offset.X) + Math.Abs(offset.Y) != 1)
            return false;

        if (!TryGetWallAt((grid, gridComp), wallTile, out wall))
            return false;

        if (!CanMountAt((grid, gridComp), clickTile))
            return false;

        gridUid = grid;
        tile = clickTile;
        side = offset.AsDirection();
        return true;
    }

    private bool TryGetWallAt(Entity<MapGridComponent> grid, Vector2i tile, out EntityUid wall)
    {
        var enumerator = _map.GetAnchoredEntitiesEnumerator(grid, grid.Comp, tile);
        while (enumerator.MoveNext(out var uid))
        {
            if (!IsWallSurface(uid.Value))
                continue;

            wall = uid.Value;
            return true;
        }

        wall = default;
        return false;
    }

    private bool IsWallSurface(EntityUid uid)
    {
        return _wallQuery.HasComp(uid) || _tag.HasTag(uid, CEWallmountSystem.WindowTag);
    }

    /// <summary>
    /// The tile has floor, nothing solid on it, and no other wallmount hanging in it.
    /// </summary>
    private bool CanMountAt(Entity<MapGridComponent> grid, Vector2i tile)
    {
        if (!_map.TryGetTileRef(grid, grid.Comp, tile, out var tileRef) || tileRef.Tile.IsEmpty)
            return false;

        if (_turf.IsTileBlocked(grid, tile, CollisionGroup.Impassable, grid.Comp))
            return false;

        return !HasWallmountAt(grid, tile);
    }

    /// <summary>
    /// Wallmounts don't collide, so they are found through the walls around the tile they are attached to,
    /// plus the ones that were just spawned and haven't attached yet.
    /// </summary>
    private bool HasWallmountAt(Entity<MapGridComponent> grid, Vector2i tile)
    {
        var enumerator = _map.GetAnchoredEntitiesEnumerator(grid, grid.Comp, tile);
        while (enumerator.MoveNext(out var uid))
        {
            if (_wallmountQuery.HasComp(uid.Value))
                return true;
        }

        foreach (var dir in CardinalDirections)
        {
            var neighbours = _map.GetAnchoredEntitiesEnumerator(grid, grid.Comp, tile + dir.ToIntVec());
            while (neighbours.MoveNext(out var neighbour))
            {
                if (!_wallmountedQuery.TryComp(neighbour.Value, out var wallmounted))
                    continue;

                foreach (var attached in wallmounted.Attached)
                {
                    if (!TerminatingOrDeleted(attached)
                        && _map.TileIndicesFor(grid, grid.Comp, Transform(attached).Coordinates) == tile)
                        return true;
                }
            }
        }

        return false;
    }
}

[Serializable, NetSerializable]
public sealed partial class CEWallmountPlaceDoAfterEvent : DoAfterEvent
{
    public NetEntity Grid;
    public Vector2i Tile;
    public Direction Side;
    public NetEntity Wall;

    public CEWallmountPlaceDoAfterEvent(NetEntity grid, Vector2i tile, Direction side, NetEntity wall)
    {
        Grid = grid;
        Tile = tile;
        Side = side;
        Wall = wall;
    }

    public override DoAfterEvent Clone()
    {
        return this;
    }

    public override bool IsDuplicate(DoAfterEvent other)
    {
        return other is CEWallmountPlaceDoAfterEvent otherPlace
               && Grid == otherPlace.Grid
               && Tile == otherPlace.Tile;
    }
}
