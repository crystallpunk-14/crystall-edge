using System.Numerics;
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
    private const byte MaxPendingFrames = 8;
    private const float ActiveAuditInterval = 0.25f;

    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SpriteSystem _sprite = default!;

    private readonly Dictionary<EntityUid, SpriteTransform> _originalTransforms = new();
    private readonly HashSet<EntityUid> _active = new();
    private readonly HashSet<EntityUid> _pending = new();
    private readonly Dictionary<EntityUid, byte> _remainingAttempts = new();
    private readonly List<EntityUid> _pendingSnapshot = new();
    private readonly List<EntityUid> _completed = new();
    private readonly List<EntityUid> _stale = new();
    private float _activeAuditAccumulator;

    public override void Initialize()
    {
        base.Initialize();
        UpdatesAfter.Add(typeof(AnimationPlayerSystem));
        SubscribeLocalEvent<AppearanceComponent, AppearanceChangeEvent>(OnAppearanceChange);
        SubscribeLocalEvent<AppearanceComponent, ComponentShutdown>(OnAppearanceShutdown);
    }

    private void OnAppearanceChange(Entity<AppearanceComponent> ent, ref AppearanceChangeEvent args)
    {
        QueueApply(ent.Owner);
    }

    private void OnAppearanceShutdown(Entity<AppearanceComponent> ent, ref ComponentShutdown args)
    {
        if (TryComp<SpriteComponent>(ent.Owner, out var sprite))
            Restore(ent.Owner, sprite);

        ClearTracking(ent.Owner);
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        _pendingSnapshot.AddRange(_pending);
        foreach (var uid in _pendingSnapshot)
        {
            if (Apply(uid) ||
                !_remainingAttempts.TryGetValue(uid, out var remaining) ||
                remaining <= 1)
            {
                _completed.Add(uid);
            }
            else
            {
                _remainingAttempts[uid] = (byte) (remaining - 1);
            }
        }

        _pendingSnapshot.Clear();

        foreach (var uid in _completed)
        {
            _pending.Remove(uid);
            _remainingAttempts.Remove(uid);
        }

        _completed.Clear();
        AuditActive(frameTime);
        ApplyActiveRotations();
    }

    /// <summary>
    /// Returns false only while an active visual is waiting for its sprite component.
    /// Appearance initial state can arrive before the sprite initial state on a client.
    /// </summary>
    private bool Apply(EntityUid uid)
    {
        if (TerminatingOrDeleted(uid))
        {
            ClearTracking(uid);
            return true;
        }

        if (!TryComp<AppearanceComponent>(uid, out var appearance))
        {
            if (TryComp<SpriteComponent>(uid, out var orphanedSprite))
                Restore(uid, orphanedSprite);

            ClearTracking(uid);
            return true;
        }

        if (!_appearance.TryGetData<bool>(uid, CEFixedSlotVisuals.Active, out var active, appearance) ||
            !active)
        {
            if (TryComp<SpriteComponent>(uid, out var inactiveSprite))
                Restore(uid, inactiveSprite);
            else
                _originalTransforms.Remove(uid);

            _active.Remove(uid);
            return true;
        }

        _active.Add(uid);
        if (!TryComp<SpriteComponent>(uid, out var sprite))
        {
            _originalTransforms.Remove(uid);
            return false;
        }

        TryComp<CEZPhysicsComponent>(uid, out var zPhysics);
        // CEZPhysics ownership is a stable prototype contract while this visual is active.
        // A runtime owner swap cannot reconstruct a clean offset baseline if another animation also owns Offset.
        if (!_originalTransforms.TryGetValue(uid, out var original) ||
            !ReferenceEquals(original.Sprite, sprite) ||
            !ReferenceEquals(original.ZPhysics, zPhysics))
        {
            original = new SpriteTransform(
                sprite,
                zPhysics,
                sprite.Offset,
                HasComp<RotationVisualsComponent>(uid) ? null : sprite.Rotation,
                zPhysics?.SpriteOffsetDefault ?? Vector2.Zero);
            _originalTransforms[uid] = original;
        }

        _appearance.TryGetData<Vector2>(uid, CEFixedSlotVisuals.Offset, out var offset, appearance);
        _appearance.TryGetData<Angle>(uid, CEFixedSlotVisuals.Rotation, out var rotation, appearance);
        if (zPhysics != null)
        {
            // CE Z-level rendering owns the whole-sprite offset each frame. Compose with its
            // canonical baseline instead of racing the pre/post-animation pipeline.
            var targetDefault = original.ZOffsetDefault + offset;
            var delta = targetDefault - zPhysics.SpriteOffsetDefault;
            zPhysics.SpriteOffsetDefault = targetDefault;
            _sprite.SetOffset((uid, sprite), sprite.Offset + delta);
        }
        else
        {
            _sprite.SetOffset((uid, sprite), original.Offset + offset);
        }

        if (original.Rotation is { } originalRotation && !HasComp<RotationVisualsComponent>(uid))
            _sprite.SetRotation((uid, sprite), originalRotation + rotation);
        return true;
    }

    private void AuditActive(float frameTime)
    {
        _activeAuditAccumulator += frameTime;
        if (_activeAuditAccumulator < ActiveAuditInterval)
            return;

        _activeAuditAccumulator = 0f;
        foreach (var uid in _active)
        {
            if (TerminatingOrDeleted(uid))
            {
                _stale.Add(uid);
                continue;
            }

            if (!TryComp<AppearanceComponent>(uid, out var appearance) ||
                !_appearance.TryGetData<bool>(uid, CEFixedSlotVisuals.Active, out var active, appearance) ||
                !active)
            {
                QueueApply(uid);
                continue;
            }

            if (!TryComp<SpriteComponent>(uid, out var sprite))
            {
                _originalTransforms.Remove(uid);
                continue;
            }

            TryComp<CEZPhysicsComponent>(uid, out var zPhysics);
            if (!_originalTransforms.TryGetValue(uid, out var original) ||
                !ReferenceEquals(original.Sprite, sprite) ||
                !ReferenceEquals(original.ZPhysics, zPhysics))
            {
                QueueApply(uid);
            }
        }

        foreach (var uid in _stale)
            ClearTracking(uid);

        _stale.Clear();
    }

    private void QueueApply(EntityUid uid)
    {
        _pending.Add(uid);
        _remainingAttempts[uid] = MaxPendingFrames;
    }

    private void ClearTracking(EntityUid uid)
    {
        _active.Remove(uid);
        _pending.Remove(uid);
        _remainingAttempts.Remove(uid);
        _originalTransforms.Remove(uid);
    }

    private void ApplyActiveRotations()
    {
        foreach (var uid in _active)
        {
            if (!_originalTransforms.TryGetValue(uid, out var original) ||
                original.Rotation is not { } originalRotation ||
                !TryComp<SpriteComponent>(uid, out var sprite) ||
                !ReferenceEquals(original.Sprite, sprite) ||
                HasComp<RotationVisualsComponent>(uid) ||
                !TryComp<AppearanceComponent>(uid, out var appearance) ||
                !_appearance.TryGetData<bool>(uid, CEFixedSlotVisuals.Active, out var active, appearance) ||
                !active)
                continue;

            _appearance.TryGetData<Angle>(uid, CEFixedSlotVisuals.Rotation, out var rotation, appearance);
            var target = originalRotation + rotation;
            if (sprite.Rotation != target)
                _sprite.SetRotation((uid, sprite), target);
        }
    }

    private void Restore(EntityUid uid, SpriteComponent sprite)
    {
        if (!_originalTransforms.Remove(uid, out var original))
            return;

        if (!ReferenceEquals(original.Sprite, sprite))
            return;

        if (original.ZPhysics != null &&
            TryComp<CEZPhysicsComponent>(uid, out var zPhysics) &&
            ReferenceEquals(original.ZPhysics, zPhysics))
        {
            var delta = original.ZOffsetDefault - zPhysics.SpriteOffsetDefault;
            zPhysics.SpriteOffsetDefault = original.ZOffsetDefault;
            _sprite.SetOffset((uid, sprite), sprite.Offset + delta);
        }
        else
        {
            _sprite.SetOffset((uid, sprite), original.Offset);
        }

        if (original.Rotation is { } originalRotation && !HasComp<RotationVisualsComponent>(uid))
            _sprite.SetRotation((uid, sprite), originalRotation);
    }

    private readonly record struct SpriteTransform(
        SpriteComponent Sprite,
        CEZPhysicsComponent? ZPhysics,
        Vector2 Offset,
        Angle? Rotation,
        Vector2 ZOffsetDefault);
}
