using Content.Client.IconSmoothing;
using Content.Shared._CE.Wallpaper;
using Robust.Client.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using static Robust.Client.GameObjects.SpriteComponent;

namespace Content.Client._CE.Wallpaper;

/// <summary>
/// Renders a wall's CEWallpaperHolderComponent as extra sprite layers on the wall's own SpriteComponent -
/// one layer per occupied side, each pointed at the same directions:4 RSI state and distinguished only by
/// a DirOffset (so a wallpaper design never needs separate art per wall side).
/// </summary>
public sealed partial class CEClientWallpaperSystem : EntitySystem
{
    [Dependency] private SpriteSystem _sprite = default!;
    [Dependency] private IPrototypeManager _proto = default!;

    private static readonly Direction[] Sides = { Direction.South, Direction.North, Direction.East, Direction.West };

    public override void Initialize()
    {
        base.Initialize();

        // IconSmoothing rebuilds its corner layers by re-appending them at the tail of the sprite's layer
        // list. Ordering our own rebuild after it (for whichever event fires both) means our wallpaper stays
        // on top instead of getting buried underneath freshly re-appended corners.
        var after = new[] { typeof(IconSmoothSystem) };
        SubscribeLocalEvent<CEWallpaperHolderComponent, ComponentStartup>(OnStartup, after: after);
        SubscribeLocalEvent<CEWallpaperHolderComponent, AfterAutoHandleStateEvent>(OnHandleState, after: after);
    }

    private void OnStartup(Entity<CEWallpaperHolderComponent> ent, ref ComponentStartup args) => Rebuild(ent);

    private void OnHandleState(Entity<CEWallpaperHolderComponent> ent, ref AfterAutoHandleStateEvent args) => Rebuild(ent);

    private void Rebuild(Entity<CEWallpaperHolderComponent> holder)
    {
        if (!TryComp<SpriteComponent>(holder.Owner, out var spriteComp))
            return;

        Entity<SpriteComponent?> sprite = (holder.Owner, spriteComp);

        foreach (var side in Sides)
        {
            _sprite.LayerMapRemove(sprite, LayerKey(side));
        }

        foreach (var (side, protoId) in holder.Comp.Layers)
        {
            if (!_proto.TryIndex(protoId, out var proto) || proto.States.Count == 0)
                continue;

            var key = LayerKey(side);
            _sprite.LayerMapReserve(sprite, key);
            _sprite.LayerSetSprite(sprite, key, new SpriteSpecifier.Rsi(proto.RsiPath, PickState(proto, holder.Owner, side)));
            _sprite.LayerSetDirOffset(sprite, key, SideOffset(side));
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

    /// <summary>
    /// Stable per-wall-and-side pick from the design's variant states, so every client renders the same
    /// texture for the same wall without needing to network which variant was chosen.
    /// </summary>
    private string PickState(CEWallpaperPrototype proto, EntityUid uid, Direction side)
    {
        var hash = HashCode.Combine(GetNetEntity(uid), side);
        var index = (int)((uint)hash % (uint)proto.States.Count);
        return proto.States[index];
    }
}
