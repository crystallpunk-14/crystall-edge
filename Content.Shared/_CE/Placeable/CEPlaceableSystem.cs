using System.Diagnostics.CodeAnalysis;
using Content.Shared._CE.ZLevels.Core.EntitySystems;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Stacks;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared._CE.Placeable;

/// <summary>
/// Places entities from <see cref="CEPlaceableComponent"/> items onto the clicked tile, facing the picked direction.
/// </summary>
public sealed partial class CEPlaceableSystem : EntitySystem
{
    [Dependency] private INetManager _net = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private ISharedAdminLogManager _adminLog = default!;
    [Dependency] private CESharedZLevelsSystem _zLevel = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedStackSystem _stack = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    [Dependency] private EntityQuery<CEPlaceableComponent> _placeableQuery = default!;
    [Dependency] private EntityQuery<MapGridComponent> _gridQuery = default!;

    [SubscribeLocalEvent]
    private void OnAfterInteract(Entity<CEPlaceableComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || !IsPlacementClick(args.User, args.Target))
            return;

        if (!TryGetTarget(ent, args.User, args.ClickLocation, out var target))
            return;

        args.Handled = true;

        if (!CanPlace(ent, target.Value, out var failed))
        {
            var popup = failed?.GetPopup(EntityManager, target.Value) ?? Loc.GetString("ce-placeable-cannot-place");
            _popup.PopupClient(popup, args.User, args.User);
            return;
        }

        if (ent.Comp.Delay <= TimeSpan.Zero)
        {
            Place(ent, target.Value);
            return;
        }

        var ev = new CEPlaceableDoAfterEvent(GetNetEntity(target.Value.Grid), target.Value.Tile, target.Value.Direction);

        // No DoAfter target: when placing below, the grid is on another map and the range check would cancel it.
        var doAfterArgs = new DoAfterArgs(EntityManager, args.User, ent.Comp.Delay, ev, ent, used: ent)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            NeedHand = true,
        };

        _doAfter.TryStartDoAfter(doAfterArgs);
    }

    [SubscribeLocalEvent]
    private void OnExamined(Entity<CEPlaceableComponent> ent, ref ExaminedEvent args)
    {
        if (ent.Comp.ExamineText is not { } text || !_proto.Resolve(ent.Comp.Prototype, out var proto))
            return;

        args.PushMarkup(Loc.GetString(text, ("name", FormattedMessage.EscapeText(proto.Name))));
    }

    [SubscribeLocalEvent]
    private void OnPlaceDoAfter(Entity<CEPlaceableComponent> ent, ref CEPlaceableDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        var gridUid = GetEntity(args.Grid);
        if (!_gridQuery.TryComp(gridUid, out var grid))
            return;

        var target = new CEPlacementTarget(args.User, ent.Comp.Prototype, (gridUid, grid), args.Tile, args.Direction);
        if (!CanPlace(ent, target))
            return;

        args.Handled = true;
        Place(ent, target);
    }

    [EventSubscription]
    private void OnRotate(CEPlaceableRotateEvent ev, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not { } user
            || !TryGetEntity(ev.Item, out var item)
            || _hands.GetActiveItem(user) != item
            || !_placeableQuery.TryComp(item, out var placeable)
            || !placeable.Rotatable)
            return;

        placeable.Direction = ev.Direction;
        Dirty(item.Value, placeable);
    }

    /// <summary>
    /// False when the click hit a loose entity other than the user, which keeps the click for itself.
    /// </summary>
    public bool IsPlacementClick(EntityUid user, EntityUid? clicked)
    {
        return clicked is not { } target || target == user || Transform(target).Anchored;
    }

    /// <summary>
    /// Finds the tile a click places into: a floored tile on the user's level, else the same tile one level below.
    /// </summary>
    public bool TryGetTarget(Entity<CEPlaceableComponent> ent, EntityUid user, EntityCoordinates clickLocation, [NotNullWhen(true)] out CEPlacementTarget? target)
    {
        target = null;

        var clickMap = _transform.ToMapCoordinates(clickLocation);
        if (clickMap.MapId == MapId.Nullspace)
            return false;

        if (_map.TryFindGridAt(clickMap, out var gridUid, out var grid)
            && _map.TryGetTileRef(gridUid, grid, clickMap.Position, out var tileRef)
            && !tileRef.Tile.IsEmpty)
        {
            target = new CEPlacementTarget(user, ent.Comp.Prototype, (gridUid, grid), tileRef.GridIndices, GetDirection(ent));
            return true;
        }

        if (!ent.Comp.AllowPlaceBelow
            || Transform(user).MapUid is not { } map
            || !_zLevel.TryMapDown((map, null), out var belowMap)
            || !_map.TryFindGridAt(belowMap.Owner, clickMap.Position, out var belowGridUid, out var belowGrid))
            return false;

        var userTile = _map.WorldToTile(belowGridUid, belowGrid, _transform.GetWorldPosition(user));
        var clickTile = _map.WorldToTile(belowGridUid, belowGrid, clickMap.Position);
        var offset = clickTile - userTile;
        if (Math.Abs(offset.X) + Math.Abs(offset.Y) != 1)
            return false;

        target = new CEPlacementTarget(user, ent.Comp.Prototype, (belowGridUid, belowGrid), clickTile, GetDirection(ent));
        return true;
    }

    /// <summary>
    /// The tile has floor and passes every condition of the item.
    /// </summary>
    public bool CanPlace(Entity<CEPlaceableComponent> ent, CEPlacementTarget target)
    {
        return CanPlace(ent, target, out _);
    }

    /// <param name="failed">The first condition that failed, or null if the tile has no floor.</param>
    public bool CanPlace(Entity<CEPlaceableComponent> ent, CEPlacementTarget target, out CEPlacementCondition? failed)
    {
        failed = null;

        if (!_map.TryGetTileRef(target.Grid, target.Grid.Comp, target.Tile, out var tileRef) || tileRef.Tile.IsEmpty)
            return false;

        foreach (var condition in ent.Comp.Conditions)
        {
            if (condition.Check(EntityManager, target))
                continue;

            failed = condition;
            return false;
        }

        return true;
    }

    private static Direction GetDirection(Entity<CEPlaceableComponent> ent)
    {
        return ent.Comp.Rotatable ? ent.Comp.Direction : Direction.South;
    }

    private void Place(Entity<CEPlaceableComponent> ent, CEPlacementTarget target)
    {
        var prototype = target.Prototype;
        var coords = _map.GridTileToLocal(target.Grid, target.Grid.Comp, target.Tile);
        _audio.PlayPredicted(ent.Comp.PlaceSound, coords, target.User);

        if (TryComp<StackComponent>(ent, out var stack) && stack.Count > 1)
            _stack.SetCount((ent.Owner, stack), stack.Count - 1);
        else
            PredictedQueueDel(ent.Owner);

        // Spawned on the server only: an anchored structure spawned in prediction would mispredict.
        if (_net.IsClient)
            return;

        var placed = SpawnAttachedTo(prototype, coords, rotation: target.Direction.ToAngle());
        _adminLog.Add(LogType.Construction, LogImpact.Low,
            $"{ToPrettyString(target.User):player} placed {ToPrettyString(placed):entity} at {coords}");
    }
}

[Serializable, NetSerializable]
public sealed partial class CEPlaceableDoAfterEvent : DoAfterEvent
{
    public NetEntity Grid;
    public Vector2i Tile;
    public Direction Direction;

    public CEPlaceableDoAfterEvent(NetEntity grid, Vector2i tile, Direction direction)
    {
        Grid = grid;
        Tile = tile;
        Direction = direction;
    }

    public override DoAfterEvent Clone()
    {
        return this;
    }

    public override bool IsDuplicate(DoAfterEvent other)
    {
        return other is CEPlaceableDoAfterEvent otherPlace
               && Grid == otherPlace.Grid
               && Tile == otherPlace.Tile;
    }
}

/// <summary>
/// Sent by the client when the user rotates the preview of the held item.
/// </summary>
[Serializable, NetSerializable]
public sealed class CEPlaceableRotateEvent : EntityEventArgs
{
    public NetEntity Item;
    public Direction Direction;

    public CEPlaceableRotateEvent(NetEntity item, Direction direction)
    {
        Item = item;
        Direction = direction;
    }
}
