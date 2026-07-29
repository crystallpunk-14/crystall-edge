using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Stacks;
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
    [Dependency] private IPrototypeManager _proto = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CEWallpaperHolderComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<CEWallpaperHolderComponent, CEWallpaperApplyDoAfterEvent>(OnApplyDoAfter);
        SubscribeLocalEvent<CEWallpaperHolderComponent, CEWallpaperRemoveDoAfterEvent>(OnRemoveDoAfter);
        SubscribeLocalEvent<CEWallpaperHolderComponent, MapInitEvent>(OnMapInit);
    }

    /// <summary>
    /// Drops any layer whose design no longer exists as a CEWallpaperPrototype - e.g. a map was saved with
    /// wallpaper that got removed/renamed in a later content update. Runs once per wall as it loads, so old
    /// maps never need to be hand-edited: the side just quietly goes back to bare wall and can be re-papered
    /// normally. The client-side renderer already tolerates unresolvable protos on its own (it just skips
    /// that layer), so this isn't needed to avoid a crash - it's here so the data doesn't carry dead
    /// references forever and the side is actually reported as free again.
    /// </summary>
    private void OnMapInit(Entity<CEWallpaperHolderComponent> holder, ref MapInitEvent args)
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

    private void OnInteractUsing(Entity<CEWallpaperHolderComponent> holder, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (TryComp<CEWallpaperComponent>(args.Used, out var wallpaper))
        {
            args.Handled = true;

            // Same design already glued to this side - re-gluing it would be a pure no-op, so don't even
            // start the DoAfter for it.
            if (GetSide(holder, args.User) is not { } side
                || (holder.Comp.Layers.TryGetValue(side, out var existing) && existing == wallpaper.Proto))
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

        if (HasComp<CEWallpaperRemoverComponent>(args.Used) && GetSide(holder, args.User) is { } side && holder.Comp.Layers.ContainsKey(side))
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

    private void OnApplyDoAfter(Entity<CEWallpaperHolderComponent> holder, ref CEWallpaperApplyDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Target == null || args.Used == null)
            return;

        if (!TryComp<CEWallpaperComponent>(args.Used, out var wallpaper) || GetSide(holder, args.User) is not { } side)
            return;

        args.Handled = true;

        // Replaces whatever was already on this side rather than adding to it - a wall only ever has one
        // layer per side, so unbounded stacking is impossible by construction.
        holder.Comp.Layers[side] = wallpaper.Proto;
        Dirty(holder);

        if (TryComp<StackComponent>(args.Used, out var stack) && stack.Count > 1)
            _stack.SetCount(args.Used.Value, stack.Count - 1);
        else
            PredictedQueueDel(args.Used.Value);
    }

    private void OnRemoveDoAfter(Entity<CEWallpaperHolderComponent> holder, ref CEWallpaperRemoveDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Target == null)
            return;

        if (GetSide(holder, args.User) is not { } side || !holder.Comp.Layers.Remove(side))
            return;

        args.Handled = true;
        Dirty(holder);
    }

    /// <summary>
    /// Which cardinal side of the wall the user is interacting from, computed in the wall's own local
    /// (rotated) frame rather than raw world-space, so it stays correct even if the wall itself has been
    /// rotated - unlike a naive world-space delta comparison.
    /// </summary>
    private Direction? GetSide(Entity<CEWallpaperHolderComponent> holder, EntityUid user)
    {
        var wallPos = _transform.GetWorldPosition(holder.Owner);
        var wallRot = _transform.GetWorldRotation(holder.Owner);
        var userPos = _transform.GetWorldPosition(user);

        var delta = userPos - wallPos;
        if (delta.LengthSquared() < 0.001f)
            return null;

        var localDelta = (-wallRot).RotateVec(delta);

        // Deliberately not Angle.GetCardinalDir() here - that method assumes the "0 = South" rotation-frame
        // convention entities use, not a plain atan2 position-delta vector, and would end up a quarter turn off.
        if (Math.Abs(localDelta.X) > Math.Abs(localDelta.Y))
            return localDelta.X > 0 ? Direction.East : Direction.West;

        return localDelta.Y > 0 ? Direction.North : Direction.South;
    }
}

[Serializable, NetSerializable]
public sealed partial class CEWallpaperApplyDoAfterEvent : SimpleDoAfterEvent;

[Serializable, NetSerializable]
public sealed partial class CEWallpaperRemoveDoAfterEvent : SimpleDoAfterEvent;
