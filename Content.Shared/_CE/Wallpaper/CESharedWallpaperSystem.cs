using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Stacks;
using Robust.Shared.Analyzers;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._CE.Wallpaper;

/// <summary>
/// Handles gluing/removing wallpaper on a wall's CEWallpaperHolderComponent. Runs on both client and server
/// (prediction) - the actual rendering of the resulting layers is purely client-side, see
/// Content.Client._CE.Wallpaper.CEClientWallpaperSystem.
/// </summary>
public sealed partial class CESharedWallpaperSystem : EntitySystem
{
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SharedStackSystem _stack = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private EntityQuery<CEWallpaperHolderComponent> _holderQuery = default!;
    [Dependency] private EntityQuery<MapGridComponent> _gridQuery = default!;

    public static readonly Direction[] Sides = { Direction.South, Direction.North, Direction.East, Direction.West };

    [SubscribeLocalEvent]
    private void OnStartup(Entity<CEWallpaperHolderComponent> holder, ref ComponentStartup args)
    {
        RemoveStaleLayers(holder);

        var ev = new CEWallpaperHolderStartupEvent();
        RaiseLocalEvent(holder, ref ev);
    }

    private void RemoveStaleLayers(Entity<CEWallpaperHolderComponent> holder)
    {
        List<Direction>? stale = null;
        foreach (var (side, protoId) in holder.Comp.Layers)
        {
            if (!_proto.HasIndex(protoId))
                (stale ??= new List<Direction>()).Add(side);
        }

        if (stale == null)
            return;

        foreach (var side in stale)
            holder.Comp.Layers.Remove(side);

        Log.Warning($"{ToPrettyString(holder.Owner)} had wallpaper referencing unknown design(s) on side(s) [{string.Join(", ", stale)}] - cleared, side(s) are free to re-paper.");
        Dirty(holder);
    }

    [SubscribeLocalEvent]
    private void OnInteractUsing(Entity<CEWallpaperHolderComponent> holder, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (TryComp<CEWallpaperComponent>(args.Used, out var wallpaper))
        {
            args.Handled = true;

            if (!TryGetSide(holder, args.User, out var applySide, out var blocked))
                return;

            if (blocked)
            {
                _popup.PopupEntity(Loc.GetString("ce-wallpaper-side-blocked"), holder, args.User);
                return;
            }

            // Same design already glued to this side - re-gluing it would be a pure no-op, so don't even
            // start the DoAfter for it.
            if (HasWallpaper(holder, applySide, wallpaper.Proto))
                return;

            var doAfterArgs = new DoAfterArgs(EntityManager, args.User, wallpaper.Delay,
                new CEWallpaperApplyDoAfterEvent(), holder, holder, args.Used)
            {
                BreakOnDamage = true,
                BreakOnMove = true,
                MovementThreshold = 0.5f,
            };

            _doAfter.TryStartDoAfter(doAfterArgs);
            return;
        }

        if (HasComp<CEWallpaperRemoverComponent>(args.Used)
            && TryGetSide(holder, args.User, out var side, out var sideBlocked)
            && !sideBlocked
            && holder.Comp.Layers.ContainsKey(side))
        {
            args.Handled = true;
            var doAfterArgs = new DoAfterArgs(EntityManager, args.User, Comp<CEWallpaperRemoverComponent>(args.Used).Delay,
                new CEWallpaperRemoveDoAfterEvent(), holder, holder, args.Used)
            {
                BreakOnDamage = true,
                BreakOnMove = true,
                MovementThreshold = 0.5f,
            };

            _doAfter.TryStartDoAfter(doAfterArgs);
        }
    }

    [SubscribeLocalEvent]
    private void OnApplyDoAfter(Entity<CEWallpaperHolderComponent> holder, ref CEWallpaperApplyDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Target == null || args.Used == null)
            return;

        if (!TryComp<CEWallpaperComponent>(args.Used, out var wallpaper)
            || !TryGetSide(holder, args.User, out var side, out var blocked)
            || blocked)
            return;

        args.Handled = true;

        // Replaces whatever was already on this side rather than adding to it - a wall only ever has one
        // layer per side, so unbounded stacking is impossible by construction.
        holder.Comp.Layers[side] = wallpaper.Proto;
        Dirty(holder);

        if (TryComp<StackComponent>(args.Used, out var stack) && stack.Count > 1)
            _stack.SetCount((args.Used.Value, stack), stack.Count - 1);
        else
            PredictedQueueDel(args.Used.Value);
    }

    [SubscribeLocalEvent]
    private void OnRemoveDoAfter(Entity<CEWallpaperHolderComponent> holder, ref CEWallpaperRemoveDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Target == null)
            return;

        if (!TryGetSide(holder, args.User, out var side, out var blocked)
            || blocked
            || !holder.Comp.Layers.Remove(side))
            return;

        args.Handled = true;
        Dirty(holder);
    }

    /// <summary>
    /// Which cardinal side of the wall the user would glue to, computed in the wall's own local (rotated)
    /// frame rather than raw world-space, so it stays correct even if the wall itself has been rotated.
    /// Of the two sides facing the user, the one along the larger axis wins; if that one faces another wall,
    /// the other facing side is used instead. <paramref name="blocked"/> is true when no facing side is open.
    /// </summary>
    public bool TryGetSide(Entity<CEWallpaperHolderComponent> holder, EntityUid user, out Direction side, out bool blocked)
    {
        side = default;
        blocked = false;

        var wallPos = _transform.GetWorldPosition(holder.Owner);
        var wallRot = _transform.GetWorldRotation(holder.Owner);
        var userPos = _transform.GetWorldPosition(user);

        var delta = userPos - wallPos;
        if (delta.LengthSquared() < 0.001f)
            return false;

        var localDelta = (-wallRot).RotateVec(delta);

        // Deliberately not Angle.GetCardinalDir() here - that method assumes the "0 = South" rotation-frame
        // convention entities use, not a plain atan2 position-delta vector, and would end up a quarter turn off.
        var horizontal = localDelta.X > 0 ? Direction.East : Direction.West;
        var vertical = localDelta.Y > 0 ? Direction.North : Direction.South;
        var horizontalFirst = Math.Abs(localDelta.X) > Math.Abs(localDelta.Y);

        side = horizontalFirst ? horizontal : vertical;
        if (!IsSideBlocked(holder.Owner, side))
            return true;

        // The user only really faces the second side when they are off-axis along it too.
        var fallback = horizontalFirst ? vertical : horizontal;
        var fallbackOffset = horizontalFirst ? localDelta.Y : localDelta.X;
        if (Math.Abs(fallbackOffset) > 0.01f && !IsSideBlocked(holder.Owner, fallback))
        {
            side = fallback;
            return true;
        }

        blocked = true;
        return true;
    }

    /// <summary>
    /// True when exactly this design is already glued to the given side.
    /// </summary>
    public bool HasWallpaper(Entity<CEWallpaperHolderComponent> holder, Direction side, ProtoId<CEWallpaperPrototype> design)
    {
        return holder.Comp.Layers.TryGetValue(side, out var existing) && existing == design;
    }

    /// <summary>
    /// True when the given side of the wall (in the wall's own frame) looks straight into another
    /// wallpaper-capable wall, so nothing glued there could ever be seen.
    /// </summary>
    public bool IsSideBlocked(EntityUid wall, Direction side)
    {
        if (!TryGetNeighbourTile(wall, side, out var grid, out var tile))
            return false;

        var enumerator = _map.GetAnchoredEntities(grid, tile);
        while (enumerator.MoveNext(out var ent))
        {
            if (ent.Value != wall && _holderQuery.HasComp(ent.Value) && !TerminatingOrDeleted(ent.Value))
                return true;
        }

        return false;
    }

    /// <summary>
    /// The grid tile next to the wall on the given side. The side is in the wall's own frame, so the wall's
    /// rotation relative to its grid is applied to get the grid-space offset.
    /// </summary>
    public bool TryGetNeighbourTile(EntityUid wall, Direction side, out Entity<MapGridComponent> grid, out Vector2i tile)
    {
        grid = default;
        tile = default;

        var xform = Transform(wall);
        if (!xform.Anchored || xform.GridUid is not { } gridUid || !_gridQuery.TryComp(gridUid, out var gridComp))
            return false;

        var offset = xform.LocalRotation.RotateVec(side.ToVec());
        grid = (gridUid, gridComp);
        tile = _map.TileIndicesFor(gridUid, gridComp, xform.Coordinates)
               + new Vector2i((int)MathF.Round(offset.X), (int)MathF.Round(offset.Y));
        return true;
    }
}

/// <summary>
/// Raised on a wall after its <see cref="CEWallpaperHolderComponent"/> has started up. The client uses it to
/// redraw this wall and its neighbours, whose open/closed sides depend on which walls exist next to them.
/// </summary>
[ByRefEvent]
public record struct CEWallpaperHolderStartupEvent;

[Serializable, NetSerializable]
public sealed partial class CEWallpaperApplyDoAfterEvent : SimpleDoAfterEvent;

[Serializable, NetSerializable]
public sealed partial class CEWallpaperRemoveDoAfterEvent : SimpleDoAfterEvent;
