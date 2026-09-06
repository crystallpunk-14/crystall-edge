using System.Numerics;
using Content.Client._CE.ZLevels.Core;
using Content.Shared._CE.EntitySlots;
using Content.Shared._CE.ZLevels.Core.Components;
using Content.Shared.Rotation;
using Robust.Client.Animations;
using Robust.Client.GameObjects;

namespace Content.Client._CE.EntitySlots;

/// <summary>
/// Applies networked fixed-slot presentation without violating container transform invariants.
/// Entities with <see cref="RotationVisualsComponent"/> retain exclusive ownership of their
/// whole-sprite rotation; authored slot rotation is ignored for those entities.
/// </summary>
public sealed partial class CEFixedSlotVisualSystem : EntitySystem
{
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();
        UpdatesAfter.Add(typeof(AnimationPlayerSystem));
        SubscribeLocalEvent<AppearanceComponent, AppearanceChangeEvent>(OnAppearanceChange);
        SubscribeLocalEvent<AppearanceComponent, ComponentShutdown>(OnAppearanceShutdown);
        SubscribeLocalEvent<CEFixedSlotVisualStateComponent, ComponentShutdown>(OnStateShutdown);
        SubscribeLocalEvent<CEFixedSlotVisualStateComponent, CESpriteVisualReleasingEvent>(
            OnSpriteVisualReleasing,
            after: [typeof(CEClientZLevelsSystem)]);
        SubscribeLocalEvent<CEFixedSlotVisualStateComponent, CEZVisualBaselineQueryEvent>(
            OnZVisualBaselineQuery);
        SubscribeLocalEvent<CEFixedSlotVisualStateComponent, CEZVisualBaselineReleasingEvent>(
            OnZVisualBaselineReleasing,
            after: [typeof(CEClientZLevelsSystem)]);
    }

    private void OnAppearanceChange(Entity<AppearanceComponent> ent, ref AppearanceChangeEvent args)
    {
        if (TryComp<CEFixedSlotVisualStateComponent>(ent.Owner, out var state) && state.Running)
        {
            state.Pending = true;
            return;
        }

        if (!TerminatingOrDeleted(ent.Owner) &&
            _appearance.TryGetData<bool>(ent.Owner, CEFixedSlotVisuals.Active, out var active, ent.Comp) && active)
            EnsureComp<CEFixedSlotVisualStateComponent>(ent.Owner).Pending = true;
    }

    private void OnAppearanceShutdown(Entity<AppearanceComponent> ent, ref ComponentShutdown args)
    {
        if (!TryComp<CEFixedSlotVisualStateComponent>(ent.Owner, out var state))
            return;

        RemCompDeferred(ent.Owner, state);
    }

    private void OnSpriteVisualReleasing(
        Entity<CEFixedSlotVisualStateComponent> ent,
        ref CESpriteVisualReleasingEvent args)
    {
        if (!ReferenceEquals(ent.Comp.OriginalSprite, args.Component))
            return;

        ClearOriginal(ent.Comp);
        ent.Comp.Pending = !TerminatingOrDeleted(ent.Owner);
    }

    private void OnStateShutdown(Entity<CEFixedSlotVisualStateComponent> ent, ref ComponentShutdown args)
    {
        Restore(ent.Owner, ent.Comp);
        ent.Comp.Pending = false;
    }

    private void OnZVisualBaselineQuery(
        Entity<CEFixedSlotVisualStateComponent> ent,
        ref CEZVisualBaselineQueryEvent args)
    {
        if (ent.Comp.OriginalSprite is not { } originalSprite ||
            !TryComp<SpriteComponent>(ent.Owner, out var sprite) ||
            !ReferenceEquals(originalSprite, sprite))
        {
            ent.Comp.Pending = true;
            return;
        }

        args.SpriteOffsetBaseline = ent.Comp.CleanOffset;
        RestoreOwnedSpriteBaseline(ent.Owner, sprite, ent.Comp, ent.Comp.CleanOffset);
        ent.Comp.Pending = true;
    }

    private void OnZVisualBaselineReleasing(
        Entity<CEFixedSlotVisualStateComponent> ent,
        ref CEZVisualBaselineReleasingEvent args)
    {
        if (!ReferenceEquals(ent.Comp.OriginalZPhysics, args.Component))
            return;

        if (TerminatingOrDeleted(ent.Owner))
        {
            ClearOriginal(ent.Comp);
            ent.Comp.Pending = false;
            return;
        }

        args.Component.SpriteOffsetDefault = ent.Comp.CleanOffset;
        if (ent.Comp.OriginalSprite is { } originalSprite &&
            TryComp<SpriteComponent>(ent.Owner, out var sprite) &&
            ReferenceEquals(originalSprite, sprite))
        {
            RestoreOwnedSpriteBaseline(
                ent.Owner,
                sprite,
                ent.Comp,
                ent.Comp.CleanOffset);
        }
        else
        {
            ClearOriginal(ent.Comp);
        }

        ent.Comp.Pending = true;
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        var query = EntityQueryEnumerator<CEFixedSlotVisualStateComponent>();
        while (query.MoveNext(out var uid, out var state))
        {
            if (!state.Running || TerminatingOrDeleted(uid))
                continue;

            Refresh(uid, state);
        }
    }

    /// <summary>
    /// Appearance initial state can arrive before the sprite initial state on a client.
    /// Keep the active visual's state until its sprite is ready, without a separate retry queue.
    /// </summary>
    private void Refresh(EntityUid uid, CEFixedSlotVisualStateComponent state)
    {
        if (!TryComp<AppearanceComponent>(uid, out var appearance) ||
            !_appearance.TryGetData<bool>(uid, CEFixedSlotVisuals.Active, out var active, appearance) ||
            !active)
        {
            RemCompDeferred(uid, state);
            return;
        }

        if (!TryComp<SpriteComponent>(uid, out var sprite) || !sprite.Running)
        {
            Restore(uid, state);
            return;
        }

        CEZPhysicsComponent? zPhysics = null;
        if (TryComp<CEZPhysicsComponent>(uid, out var candidateZPhysics) && candidateZPhysics.Running)
            zPhysics = candidateZPhysics;
        // The CEZ owner events preserve the canonical baseline during normal add/remove lifecycle.
        // This identity check also recovers safely if another system replaces either component.
        if (!ReferenceEquals(state.OriginalSprite, sprite) ||
            !ReferenceEquals(state.OriginalZPhysics, zPhysics))
        {
            Restore(uid, state);
            state.OriginalSprite = sprite;
            state.OriginalZPhysics = zPhysics;
            state.CleanOffset = zPhysics?.SpriteOffsetDefault ?? sprite.Offset;
            state.OriginalRotation = HasComp<RotationVisualsComponent>(uid) ? null : sprite.Rotation;
            state.Pending = true;
        }

        // Only rewrite the offset when slot data or its visual owner changes.
        // Stable visuals must preserve offsets written by the animation player this frame.
        if (state.Pending)
        {
            _appearance.TryGetData<Vector2>(uid, CEFixedSlotVisuals.Offset, out var offset, appearance);
            if (zPhysics != null)
            {
                // CE Z-level rendering owns the whole-sprite offset each frame. Compose with its
                // canonical baseline instead of racing the pre/post-animation pipeline.
                var targetDefault = state.CleanOffset + offset;
                var delta = targetDefault - zPhysics.SpriteOffsetDefault;
                zPhysics.SpriteOffsetDefault = targetDefault;
                _sprite.SetOffset((uid, sprite), sprite.Offset + delta);
            }
            else
            {
                _sprite.SetOffset((uid, sprite), state.CleanOffset + offset);
            }

            state.Pending = false;
        }

        if (state.OriginalRotation is not { } originalRotation || HasComp<RotationVisualsComponent>(uid))
            return;

        _appearance.TryGetData<Angle>(uid, CEFixedSlotVisuals.Rotation, out var rotation, appearance);
        var target = originalRotation + rotation;
        if (sprite.Rotation != target)
            _sprite.SetRotation((uid, sprite), target);
    }

    private void Restore(EntityUid uid, CEFixedSlotVisualStateComponent state)
    {
        if (state.OriginalSprite is { } originalSprite &&
            TryComp<SpriteComponent>(uid, out var sprite) &&
            ReferenceEquals(originalSprite, sprite))
        {
            Restore(uid, sprite, state);
            return;
        }

        if (state.OriginalZPhysics is { } originalZPhysics &&
            TryComp<CEZPhysicsComponent>(uid, out var zPhysics) &&
            ReferenceEquals(originalZPhysics, zPhysics))
        {
            zPhysics.SpriteOffsetDefault = state.CleanOffset;
        }

        ClearOriginal(state);
    }

    private void Restore(EntityUid uid, SpriteComponent sprite, CEFixedSlotVisualStateComponent state)
    {
        if (!ReferenceEquals(state.OriginalSprite, sprite))
            return;

        if (state.OriginalZPhysics != null &&
            TryComp<CEZPhysicsComponent>(uid, out var zPhysics) &&
            ReferenceEquals(state.OriginalZPhysics, zPhysics))
        {
            var delta = state.CleanOffset - zPhysics.SpriteOffsetDefault;
            zPhysics.SpriteOffsetDefault = state.CleanOffset;
            _sprite.SetOffset((uid, sprite), sprite.Offset + delta);
        }
        else
        {
            _sprite.SetOffset((uid, sprite), state.CleanOffset);
        }

        if (state.OriginalRotation is { } originalRotation && !HasComp<RotationVisualsComponent>(uid))
            _sprite.SetRotation((uid, sprite), originalRotation);

        ClearOriginal(state);
    }

    private void RestoreOwnedSpriteBaseline(
        EntityUid uid,
        SpriteComponent sprite,
        CEFixedSlotVisualStateComponent state,
        Vector2 cleanOffset)
    {
        _sprite.SetOffset((uid, sprite), cleanOffset);
        if (state.OriginalRotation is { } originalRotation && !HasComp<RotationVisualsComponent>(uid))
            _sprite.SetRotation((uid, sprite), originalRotation);

        ClearOriginal(state);
    }

    private static void ClearOriginal(CEFixedSlotVisualStateComponent state)
    {
        state.OriginalSprite = null;
        state.OriginalZPhysics = null;
        state.CleanOffset = default;
        state.OriginalRotation = null;
    }
}
