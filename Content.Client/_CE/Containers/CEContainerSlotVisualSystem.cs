using System.Numerics;
using Content.Client._CE.ZLevels.Core;
using Content.Shared._CE.Containers;
using Content.Shared._CE.ZLevels.Core.Components;
using Content.Shared.Rotation;
using Robust.Client.GameObjects;

namespace Content.Client._CE.Containers;

/// <summary>
/// Applies networked fixed-slot presentation without violating container transform invariants.
/// Entities with <see cref="RotationVisualsComponent"/> retain exclusive ownership of their
/// whole-sprite rotation; authored slot rotation is ignored for those entities.
/// </summary>
public sealed partial class CEContainerSlotVisualSystem : EntitySystem
{
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();
        UpdatesBefore.Add(typeof(CEClientZLevelsPreAnimSystem));
    }

    [SubscribeLocalEvent]
    private void OnAppearanceChange(Entity<AppearanceComponent> ent, ref AppearanceChangeEvent args)
    {
        if (TryComp<CEContainerSlotVisualStateComponent>(ent.Owner, out var state) && state.Running)
        {
            state.Pending = true;
            return;
        }

        if (!TerminatingOrDeleted(ent.Owner) &&
            _appearance.TryGetData<bool>(ent.Owner, CEContainerSlotVisuals.Active, out var active, ent.Comp) && active)
            EnsureComp<CEContainerSlotVisualStateComponent>(ent.Owner).Pending = true;
    }

    [SubscribeLocalEvent]
    private void OnAppearanceShutdown(Entity<AppearanceComponent> ent, ref ComponentShutdown args)
    {
        if (!TryComp<CEContainerSlotVisualStateComponent>(ent.Owner, out var state))
            return;

        RemCompDeferred(ent.Owner, state);
    }

    [SubscribeLocalEvent]
    private void OnStateShutdown(Entity<CEContainerSlotVisualStateComponent> ent, ref ComponentShutdown args)
    {
        Restore(ent.Owner, ent.Comp);
        ent.Comp.Pending = false;
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        var query = EntityQueryEnumerator<CEContainerSlotVisualStateComponent>();
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
    private void Refresh(EntityUid uid, CEContainerSlotVisualStateComponent state)
    {
        if (!TryComp<AppearanceComponent>(uid, out var appearance) ||
            !_appearance.TryGetData<bool>(uid, CEContainerSlotVisuals.Active, out var active, appearance) ||
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
        // Keep slot state local; the existing Z-level renderer owns its baseline.
        if (!ReferenceEquals(state.OriginalSprite, sprite) ||
            !ReferenceEquals(state.OriginalZPhysics, zPhysics))
        {
            var cleanOffset = ReferenceEquals(state.OriginalSprite, sprite)
                ? state.CleanOffset
                : sprite.Offset;
            Restore(uid, state);
            state.OriginalSprite = sprite;
            state.OriginalZPhysics = zPhysics;
            state.CleanOffset = cleanOffset;
            state.OriginalRotation = HasComp<RotationVisualsComponent>(uid) ? null : sprite.Rotation;
            state.Pending = true;
        }

        // Only rewrite the offset when slot data or its visual owner changes.
        // Stable visuals must preserve offsets written by the animation player this frame.
        if (state.Pending)
        {
            _appearance.TryGetData<Vector2>(uid, CEContainerSlotVisuals.Offset, out var offset, appearance);
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

        _appearance.TryGetData<Angle>(uid, CEContainerSlotVisuals.Rotation, out var rotation, appearance);
        var target = originalRotation + rotation;
        if (sprite.Rotation != target)
            _sprite.SetRotation((uid, sprite), target);
    }

    private void Restore(EntityUid uid, CEContainerSlotVisualStateComponent state)
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

    private void Restore(EntityUid uid, SpriteComponent sprite, CEContainerSlotVisualStateComponent state)
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

    private static void ClearOriginal(CEContainerSlotVisualStateComponent state)
    {
        state.OriginalSprite = null;
        state.OriginalZPhysics = null;
        state.CleanOffset = default;
        state.OriginalRotation = null;
    }
}
