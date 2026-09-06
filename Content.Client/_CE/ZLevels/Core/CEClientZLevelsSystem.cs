/*
 * This file is sublicensed under MIT License
 * https://github.com/space-wizards/space-station-14/blob/master/LICENSE.TXT
 */

using System.Numerics;
using Content.Client._CE.ZLevels.Core.Overlays;
using Content.Shared._CE.ZLevels.Core.Components;
using Content.Shared._CE.ZLevels.Core.EntitySystems;
using Content.Shared.Camera;
using Content.Shared.StatusEffectNew.Components;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.GameObjects;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;

namespace Content.Client._CE.ZLevels.Core;

/// <summary>
/// Only process Eye offset and drawdepth on clientside
/// </summary>
public sealed partial class CEClientZLevelsSystem : CESharedZLevelsSystem
{
    [Dependency] private IOverlayManager _overlay = default!;
    [Dependency] private IEyeManager _eye = default!;
    [Dependency] private SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();
        _overlay.AddOverlay(new CEZLevelBlurOverlay());
        UpdatesBefore.Add(typeof(CEClientZLevelsPreAnimSystem));

        SubscribeLocalEvent<CEZPhysicsComponent, ComponentStartup>(OnZPhysicsStartup);
        SubscribeLocalEvent<CEZVisualBaselineStateComponent, CEZVisualBaselineReleasingEvent>(
            OnZVisualBaselineReleasing);
        SubscribeLocalEvent<CEZVisualBaselineStateComponent, CESpriteVisualReleasingEvent>(
            OnSpriteVisualReleasing);
        SubscribeLocalEvent<CEZPhysicsComponent, GetEyeOffsetEvent>(OnEyeOffset);
    }

    private void OnEyeOffset(Entity<CEZPhysicsComponent> ent, ref GetEyeOffsetEvent args)
    {
        Angle rotation = _eye.CurrentEye.Rotation * -1;
        var localPosition = GetVisualLocalPosition(ent, ent.Comp, Transform(ent), ZPhysicsQuery);
        var offset = rotation.RotateVec(new Vector2(0, localPosition * ZLevelOffset));
        args.Offset += offset;
    }

    /// <summary>
    /// Entities riding/parented onto another Z-physics body (e.g. a rider buckled to a flying vehicle)
    /// never step their own LocalPosition, so they must visually follow the parent's height instead.
    /// </summary>
    internal static float GetVisualLocalPosition(EntityUid uid, CEZPhysicsComponent zPhys, TransformComponent xform, EntityQuery<CEZPhysicsComponent> zPhysicsQuery)
    {
        if (xform.ParentUid != xform.MapUid && zPhysicsQuery.TryComp(xform.ParentUid, out var parentZPhys))
            return parentZPhys.LocalPosition;

        return zPhys.LocalPosition;
    }

    private void OnZPhysicsStartup(Entity<CEZPhysicsComponent> ent, ref ComponentStartup args)
    {
        if (TryComp<SpriteComponent>(ent.Owner, out var sprite) && sprite.Running)
            EnsureVisualBaseline(ent.Owner, ent.Comp, sprite);
    }

    private void OnZVisualBaselineReleasing(
        Entity<CEZVisualBaselineStateComponent> ent,
        ref CEZVisualBaselineReleasingEvent args)
    {
        if (!ReferenceEquals(ent.Comp.ZPhysics, args.Component))
            return;

        RestoreVisualBaseline(ent.Owner, ent.Comp);
        RemCompDeferred(ent.Owner, ent.Comp);
    }

    private void OnSpriteVisualReleasing(
        Entity<CEZVisualBaselineStateComponent> ent,
        ref CESpriteVisualReleasingEvent args)
    {
        if (!ReferenceEquals(ent.Comp.Sprite, args.Component))
            return;

        RestoreVisualBaseline(ent.Owner, ent.Comp);
        RemCompDeferred(ent.Owner, ent.Comp);
    }

    internal void EnsureVisualBaseline(
        EntityUid uid,
        CEZPhysicsComponent zPhysics,
        SpriteComponent sprite)
    {
        var state = EnsureComp<CEZVisualBaselineStateComponent>(uid);
        if (ReferenceEquals(state.ZPhysics, zPhysics) && ReferenceEquals(state.Sprite, sprite))
            return;

        RestoreVisualBaseline(uid, state);

        var baseline = new CEZVisualBaselineQueryEvent(zPhysics, sprite.Offset);
        RaiseLocalEvent(uid, ref baseline);

        state.ZPhysics = zPhysics;
        state.Sprite = sprite;
        state.SpriteOffsetBaseline = baseline.SpriteOffsetBaseline;
        state.DrawDepthBaseline = sprite.DrawDepth;

        if (sprite.SnapCardinals)
            return;

        zPhysics.DrawDepthDefault = state.DrawDepthBaseline;
        zPhysics.SpriteOffsetDefault = state.SpriteOffsetBaseline;
    }

    private void RestoreVisualBaseline(EntityUid uid, CEZVisualBaselineStateComponent state)
    {
        if (state.ZPhysics is { } originalZPhysics &&
            TryComp<CEZPhysicsComponent>(uid, out var zPhysics) &&
            ReferenceEquals(originalZPhysics, zPhysics))
        {
            zPhysics.SpriteOffsetDefault = state.SpriteOffsetBaseline;
            zPhysics.DrawDepthDefault = state.DrawDepthBaseline;
        }

        if (state.Sprite is not { } originalSprite ||
            !TryComp<SpriteComponent>(uid, out var sprite) ||
            !ReferenceEquals(originalSprite, sprite))
            return;

        _sprite.SetOffset((uid, sprite), state.SpriteOffsetBaseline);
        _sprite.SetDrawDepth((uid, sprite), state.DrawDepthBaseline);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _overlay.RemoveOverlay<CEZLevelBlurOverlay>();
    }
}

/// <summary>
/// Pre-animation pass for Z-level visuals.
/// Runs its <see cref="FrameUpdate"/> BEFORE <see cref="AnimationPlayerSystem"/> every render frame,
/// resetting <see cref="SpriteComponent.Offset"/> to the entity's clean base (no Z).
/// This prevents Z from accumulating across frames when no animation writes to the offset
/// (e.g. entities that only animate scale, like SlimeIceBig).
/// </summary>
internal sealed partial class CEClientZLevelsPreAnimSystem : EntitySystem
{
    [Dependency] private CEClientZLevelsSystem _zLevels = default!;
    [Dependency] private SpriteSystem _sprite = default!;
    [Dependency] private EntityQuery<MapGridComponent> _mapGridQuery = default!;
    [Dependency] private EntityQuery<CEZPhysicsComponent> _zPhysQuery = default!;

    public override void Initialize()
    {
        base.Initialize();
        UpdatesBefore.Add(typeof(AnimationPlayerSystem));
    }

    public override void FrameUpdate(float frameTime)
    {
        // Phase 1 (per render frame): strip any Z left from last frame so the animation player
        // always starts from a Z-free base, and Phase 2 can add exactly one Z contribution.
        var query = EntityQueryEnumerator<CEZPhysicsComponent, SpriteComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var zPhys, out var sprite, out var xform))
        {
            // Deferred removal leaves stopped components in queries until the next cull.
            // Their shutdown handlers already released the baseline; do not capture it again.
            if (!zPhys.Running || !sprite.Running)
                continue;

            _zLevels.EnsureVisualBaseline(uid, zPhys, sprite);

            var localPosition = CEClientZLevelsSystem.GetVisualLocalPosition(uid, zPhys, xform, _zPhysQuery);
            _sprite.SetOffset((uid, sprite), zPhys.SpriteOffsetDefault);
            _sprite.SetDrawDepth((uid, sprite), localPosition > 0 ? (int)Shared.DrawDepth.DrawDepth.OverMobs : zPhys.DrawDepthDefault);
        }

        // Set parent-synced status effect offsets to the parent's current Z value each frame — prevents accumulation.
        var syncQuery = EntityQueryEnumerator<StatusEffectComponent, SpriteComponent, TransformComponent>();
        while (syncQuery.MoveNext(out var uid, out _, out var sprite, out var xform))
        {
            var parent = xform.ParentUid;
            if (_mapGridQuery.HasComp(parent))
                continue;
            if (!_zPhysQuery.TryComp(parent, out var parentZPhys))
                continue;
            var zOffset = new Vector2(0, parentZPhys.LocalPosition * CESharedZLevelsSystem.ZLevelOffset);
            _sprite.SetOffset((uid, sprite), zOffset);
        }
    }
}

/// <summary>
/// Post-animation pass for Z-level visuals.
/// Runs its <see cref="FrameUpdate"/> AFTER <see cref="AnimationPlayerSystem"/> so that
/// whatever offset the animation player wrote to <see cref="SpriteComponent.Offset"/> this frame
/// (loop animation, one-shot swing, idle bob, etc.) is preserved, and the Z-height contribution
/// is simply added on top.  No animation code needs to know about Z levels.
/// </summary>
internal sealed partial class CEClientZLevelsPostAnimSystem : EntitySystem
{
    [Dependency] private SpriteSystem _sprite = default!;
    [Dependency] private SharedTransformSystem _xform = default!;
    [Dependency] private EntityQuery<CEZPhysicsComponent> _zPhysQuery = default!;

    public override void Initialize()
    {
        base.Initialize();
        UpdatesAfter.Add(typeof(AnimationPlayerSystem));
    }

    public override void FrameUpdate(float frameTime)
    {
        // Phase 2: add the Z-height contribution on top of the animation-player's output.
        // At this point sprite.Offset == animationValue (or SpriteOffsetDefault if no anim ran).
        // The offset is counter-rotated by the entity's world angle so it always points world-up,
        // preventing it from orbiting the pivot when the entity has angular velocity (e.g. shurikens).
        var query = EntityQueryEnumerator<CEZPhysicsComponent, SpriteComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var zPhys, out var sprite, out var xform))
        {
            if (!zPhys.Running || !sprite.Running)
                continue;

            var localPosition = CEClientZLevelsSystem.GetVisualLocalPosition(uid, zPhys, xform, _zPhysQuery);
            var rawZ = new Vector2(0, localPosition * CESharedZLevelsSystem.ZLevelOffset);
            Vector2 zOffset;
            if (sprite.NoRotation)
                zOffset = rawZ;
            else
                zOffset = new Angle(-_xform.GetWorldRotation(xform)).RotateVec(rawZ);
            _sprite.SetOffset((uid, sprite), sprite.Offset + zOffset);
        }
    }
}
