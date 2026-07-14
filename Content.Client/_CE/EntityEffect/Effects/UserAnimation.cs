using System.Numerics;
using Content.Shared._CE.EntityEffect;
using Content.Shared._CE.EntityEffect.Effects;
using Content.Shared._CE.ZLevels.Core.Components;
using Robust.Client.GameObjects;
using Robust.Shared.Timing;

namespace Content.Client._CE.EntityEffect.Effects;

/// <summary>
/// Tracks in-progress <see cref="UserAnimation"/> channels on an entity.
/// Stores the original sprite property values per channel so they can be restored on completion.
/// Only present while at least one channel is actively animating.
/// </summary>
[RegisterComponent]
public sealed partial class CEUserSpriteAnimationComponent : Component
{
    /// <summary>Original <see cref="SpriteComponent.Offset"/> before the animation started.</summary>
    public Vector2? OriginalOffset;

    /// <summary>Original <see cref="SpriteComponent.Rotation"/> before the animation started.</summary>
    public Angle? OriginalRotation;

    /// <summary>Original <see cref="SpriteComponent.Scale"/> before the animation started.</summary>
    public Vector2? OriginalScale;

    /// <summary>Original <see cref="SpriteComponent.Color"/> before the animation started.</summary>
    public Color? OriginalColor;
}

/// <summary>
/// Handles <see cref="UserAnimation"/> entity effects: plays sprite animations directly on the user entity
/// and restores original values when each channel finishes.
/// </summary>
public sealed partial class CEUserAnimationEffectSystem : CEEntityEffectSystem<UserAnimation>
{
    private const string OffsetKey = "ce-user-anim-offset";
    private const string RotationKey = "ce-user-anim-rotation";
    private const string ScaleKey = "ce-user-anim-scale";
    private const string ColorKey = "ce-user-anim-color";

    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private AnimationPlayerSystem _animPlayer = default!;
    [Dependency] private SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CEUserSpriteAnimationComponent, AnimationCompletedEvent>(OnAnimCompleted);
    }

    protected override void Effect(ref CEEntityEffectEvent<UserAnimation> args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        if (_timing.ApplyingState)
            return;

        var entity = args.Args.Source;

        if (!TryComp<SpriteComponent>(entity, out var sprite))
            return;

        var effect = args.Effect;
        var speedMult = 1f / args.Args.Speed;
        var comp = EnsureComp<CEUserSpriteAnimationComponent>(entity);

        if (effect.OffsetAnimation.Count > 0)
        {
            if (comp.OriginalOffset == null)
            {
                // Use Z-free base: sprite.Offset at tick time includes jump height from post-anim; restoring it would double the Z.
                comp.OriginalOffset = TryComp<CEZPhysicsComponent>(entity, out var zPhys)
                    ? zPhys.SpriteOffsetDefault
                    : sprite.Offset;
            }
            _animPlayer.Stop(entity, OffsetKey);
            _animPlayer.Play(entity, CEAnimationTrackBuilders.BuildOffsetAnimation(effect.OffsetAnimation, speedMult), OffsetKey);
        }

        if (effect.RotationAnimation.Count > 0)
        {
            comp.OriginalRotation ??= sprite.Rotation;
            _animPlayer.Stop(entity, RotationKey);
            _animPlayer.Play(entity, CEAnimationTrackBuilders.BuildRotationAnimation(effect.RotationAnimation, speedMult), RotationKey);
        }

        if (effect.ScaleAnimation.Count > 0)
        {
            comp.OriginalScale ??= sprite.Scale;
            _animPlayer.Stop(entity, ScaleKey);
            _animPlayer.Play(entity, CEAnimationTrackBuilders.BuildScaleAnimation(effect.ScaleAnimation, speedMult), ScaleKey);
        }

        if (effect.ColorAnimation.Count > 0)
        {
            comp.OriginalColor ??= effect.RestoreColor;
            _animPlayer.Stop(entity, ColorKey);
            _animPlayer.Play(entity, CEAnimationTrackBuilders.BuildColorAnimation(effect.ColorAnimation, speedMult), ColorKey);
        }
    }

    private void OnAnimCompleted(Entity<CEUserSpriteAnimationComponent> ent, ref AnimationCompletedEvent args)
    {
        // Manually stopped (new animation about to start): keep original values, do not restore yet.
        if (!args.Finished)
            return;

        if (!TryComp<SpriteComponent>(ent, out var sprite))
            return;

        switch (args.Key)
        {
            case OffsetKey:
                if (ent.Comp.OriginalOffset is { } off)
                    _sprite.SetOffset((ent.Owner, sprite), off);
                ent.Comp.OriginalOffset = null;
                break;

            case RotationKey:
                if (ent.Comp.OriginalRotation is { } rot)
                    _sprite.SetRotation((ent.Owner, sprite), rot);
                ent.Comp.OriginalRotation = null;
                break;

            case ScaleKey:
                if (ent.Comp.OriginalScale is { } scale)
                    _sprite.SetScale((ent.Owner, sprite), scale);
                ent.Comp.OriginalScale = null;
                break;

            case ColorKey:
                if (ent.Comp.OriginalColor is { } color)
                    _sprite.SetColor((ent.Owner, sprite), color);
                ent.Comp.OriginalColor = null;
                break;
        }

        // Clean up the tracking component once every channel has been restored.
        if (ent.Comp.OriginalOffset == null
            && ent.Comp.OriginalRotation == null
            && ent.Comp.OriginalScale == null
            && ent.Comp.OriginalColor == null)
        {
            RemComp<CEUserSpriteAnimationComponent>(ent);
        }
    }
}
