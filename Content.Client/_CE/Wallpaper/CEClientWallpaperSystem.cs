using Content.Client.Gameplay;
using Content.Client.Hands.Systems;
using Content.Client.IconSmoothing;
using Content.Shared._CE.Wallpaper;
using Content.Shared.Interaction;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Client.State;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using static Robust.Client.GameObjects.SpriteComponent;

namespace Content.Client._CE.Wallpaper;

public sealed partial class CEClientWallpaperSystem : EntitySystem
{
    [Dependency] private SpriteSystem _sprite = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private HandsSystem _hands = default!;
    [Dependency] private SharedInteractionSystem _interaction = default!;
    [Dependency] private CESharedWallpaperSystem _wallpaper = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private IInputManager _input = default!;
    [Dependency] private IEyeManager _eye = default!;
    [Dependency] private IStateManager _state = default!;

    [Dependency] private EntityQuery<CEWallpaperHolderComponent> _holderQuery = default!;
    [Dependency] private EntityQuery<MapGridComponent> _gridQuery = default!;

    private static readonly Color PreviewColor = Color.White.WithAlpha(0.5f);
    private static readonly Color BlockedPreviewColor = new(1f, 0.3f, 0.3f, 0.5f);

    private WallpaperPreview? _preview;

    [SubscribeLocalEvent(after: new[] { typeof(IconSmoothSystem) })]
    private void OnHandleState(Entity<CEWallpaperHolderComponent> ent, ref AfterAutoHandleStateEvent args) => Rebuild(ent);

    [SubscribeLocalEvent]
    private void OnHolderStartup(Entity<CEWallpaperHolderComponent> ent, ref CEWallpaperHolderStartupEvent args)
    {
        Rebuild(ent);
        RebuildNeighbours(ent);
    }

    [SubscribeLocalEvent]
    private void OnAnchorChanged(Entity<CEWallpaperHolderComponent> ent, ref AnchorStateChangedEvent args)
    {
        if (!TerminatingOrDeleted(ent))
            Rebuild(ent);

        RebuildNeighbours(ent);
    }

    [SubscribeLocalEvent]
    private void OnHolderShutdown(Entity<CEWallpaperHolderComponent> ent, ref ComponentShutdown args) => RebuildNeighbours(ent);

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        var target = FindPreviewTarget();
        if (target == _preview)
            return;

        var previous = _preview;
        _preview = target;

        if (previous is { } old && !TerminatingOrDeleted(old.Wall) && _holderQuery.TryComp(old.Wall, out var oldHolder))
            Rebuild((old.Wall, oldHolder));

        if (target is { } current && _holderQuery.TryComp(current.Wall, out var holder))
            Rebuild((current.Wall, holder));
    }

    private WallpaperPreview? FindPreviewTarget()
    {
        if (_player.LocalEntity is not { } player
            || _hands.GetActiveItem(player) is not { } item
            || !TryComp<CEWallpaperComponent>(item, out var roll)
            || _state.CurrentState is not GameplayStateBase screen)
            return null;

        var mouse = _eye.PixelToMap(_input.MouseScreenPosition);
        if (mouse.MapId == MapId.Nullspace
            || screen.GetClickedEntity(mouse) is not { } wall
            || !_holderQuery.TryComp(wall, out var holder)
            || !_interaction.InRangeUnobstructed(player, wall)
            || !_wallpaper.TryGetSide((wall, holder), player, out var side, out var blocked))
            return null;

        // Same design already glued there - clicking would do nothing, so there is nothing to preview.
        if (!blocked && _wallpaper.HasWallpaper((wall, holder), side, roll.Proto))
            return null;

        return new WallpaperPreview(wall, side, roll.Proto, blocked);
    }

    private void Rebuild(Entity<CEWallpaperHolderComponent> holder)
    {
        if (!TryComp<SpriteComponent>(holder.Owner, out var spriteComp))
            return;

        Entity<SpriteComponent?> sprite = (holder.Owner, spriteComp);

        // RemoveLayer, not LayerMapRemove - the latter only drops the key and would leave the old layer drawn.
        foreach (var side in CESharedWallpaperSystem.Sides)
        {
            _sprite.RemoveLayer(sprite, LayerKey(side), logMissing: false);
        }

        WallpaperPreview? preview = _preview is { } p && p.Wall == holder.Owner ? p : null;

        foreach (var (side, protoId) in holder.Comp.Layers)
        {
            // The ghost temporarily replaces whatever is glued to the previewed side.
            if (preview is { } ghostSide && ghostSide.Side == side)
                continue;

            if (_wallpaper.IsSideBlocked(holder.Owner, side))
                continue;

            AddLayer(sprite, side, protoId, null);
        }

        if (preview is { } ghost)
            AddLayer(sprite, ghost.Side, ghost.Proto, ghost.Blocked ? BlockedPreviewColor : PreviewColor);
    }

    private void AddLayer(Entity<SpriteComponent?> sprite, Direction side, ProtoId<CEWallpaperPrototype> protoId, Color? color)
    {
        if (!_proto.TryIndex(protoId, out var proto) || proto.States.Count == 0)
            return;

        var key = LayerKey(side);
        _sprite.LayerMapReserve(sprite, key);
        _sprite.LayerSetSprite(sprite, key, new SpriteSpecifier.Rsi(proto.RsiPath, PickState(proto, sprite.Owner, side)));
        _sprite.LayerSetDirOffset(sprite, key, SideOffset(side));

        if (color is { } tint)
            _sprite.LayerSetColor(sprite, key, tint);
    }

    /// <summary>
    /// Whether a side counts as blocked depends on the walls next to it, so whenever a wall appears or goes
    /// away, the walls around it have to re-check their sides.
    /// </summary>
    private void RebuildNeighbours(EntityUid wall)
    {
        var xform = Transform(wall);
        if (xform.GridUid is not { } gridUid || !_gridQuery.TryComp(gridUid, out var grid))
            return;

        var tile = _map.TileIndicesFor(gridUid, grid, xform.Coordinates);
        foreach (var side in CESharedWallpaperSystem.Sides)
        {
            var enumerator = _map.GetAnchoredEntities((gridUid, grid), tile.Offset(side));
            while (enumerator.MoveNext(out var neighbour))
            {
                if (neighbour.Value != wall && _holderQuery.TryComp(neighbour.Value, out var holder))
                    Rebuild((neighbour.Value, holder));
            }
        }
    }

    private static string LayerKey(Direction side) => $"ce-wallpaper-{side}";

    /// <summary>
    /// The base art is authored facing south (the default/front side). Every other side reuses the exact
    /// same directions:4 state, just relabelled via DirOffset instead of needing its own art.
    /// </summary>
    private static DirectionOffset SideOffset(Direction side) => side switch
    {
        Direction.South => DirectionOffset.None,
        Direction.East => DirectionOffset.CounterClockwise,
        Direction.North => DirectionOffset.Flip,
        Direction.West => DirectionOffset.Clockwise,
        _ => DirectionOffset.None,
    };

    private string PickState(CEWallpaperPrototype proto, EntityUid uid, Direction side)
    {
        var hash = HashCode.Combine(GetNetEntity(uid), side);
        var index = (int)((uint)hash % (uint)proto.States.Count);
        return proto.States[index];
    }

    private readonly record struct WallpaperPreview(
        EntityUid Wall,
        Direction Side,
        ProtoId<CEWallpaperPrototype> Proto,
        bool Blocked);
}
