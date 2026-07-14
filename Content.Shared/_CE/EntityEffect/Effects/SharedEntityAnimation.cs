using System.Numerics;
using Content.Shared._CE.Animation.Core.Prototypes;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._CE.EntityEffect.Effects;

/// <summary>
/// Abstract base for entity animation effects that spawn a client-side visual entity with customizable animations.
/// Logic is handled by client-side <c>CEEntityAnimationEffectSystem</c>.
/// Server-side, the animation system sends a network event for non-predicting clients.
/// Use <see cref="UsedEntityAnimation"/> to copy sprite from the action's container item,
/// or <see cref="ActiveHandEntityAnimation"/> to resolve sprite from the user's active hand.
/// </summary>
public abstract partial class EntityAnimation : CEEntityEffectBase<EntityAnimation>
{
    protected EntityAnimation()
    {
        EffectTarget = CEEffectTarget.User;
    }

    /// <summary>
    /// Clientside VFX entity that visuals we spawn and animate. If null, the sprite is copied from the used item entity.
    /// </summary>
    [DataField]
    public EntProtoId? DummyEntity;

    /// <summary>
    /// Whether the spawned visual entity should follow the user's position.
    /// </summary>
    [DataField]
    public bool FollowUser = true;

    /// <summary>
    /// Keyframes for animating the sprite's offset position over time.
    /// If empty, sprite stays at the initial offset position.
    /// </summary>
    [DataField]
    public List<CEOffsetKeyframe> OffsetAnimation = new();

    /// <summary>
    /// Keyframes for animating the sprite's rotation over time.
    /// If empty, sprite stays at the initial rotation.
    /// </summary>
    [DataField]
    public List<CERotationKeyframe> RotationAnimation = new();

    /// <summary>
    /// Keyframes for animating the sprite's color/alpha over time.
    /// If empty, sprite stays at full opacity. Set alpha to 0 for fade-out effects.
    /// </summary>
    [DataField]
    public List<CEColorKeyframe> ColorAnimation = new();

    /// <summary>
    /// Keyframes for animating the sprite's scale over time.
    /// If empty, sprite stays at default (1, 1) scale
    /// </summary>
    [DataField]
    public List<CEScaleKeyFrame> ScaleAnimation = new();
}

/// <summary>
/// Copies sprite from the action's container item (<see cref="CEEntityEffectArgs.Used"/>).
/// If <see cref="EntityAnimation.DummyEntity"/> is set, it overrides the item sprite.
/// </summary>
public sealed partial class UsedEntityAnimation : EntityAnimation;

/// <summary>
/// Resolves the sprite from the user's active hand item instead of <see cref="CEEntityEffectArgs.Used"/>.
/// If <see cref="EntityAnimation.DummyEntity"/> is set, it overrides the active hand sprite.
/// </summary>
public sealed partial class ActiveHandEntityAnimation : EntityAnimation;

/// <summary>
/// Resolves the sprite from the user's off-hand (non-active hand) item.
/// Used in dual-wield animations to show the off-hand weapon's swing independently.
/// If <see cref="EntityAnimation.DummyEntity"/> is set, it overrides the off-hand sprite.
/// </summary>
public sealed partial class OffHandEntityAnimation : EntityAnimation;


/// <summary>
/// Network event sent to non-predicting clients to display visual effects
/// that were already processed on the predicting client.
/// </summary>
[Serializable, NetSerializable]
public sealed class CEEntityAnimationEvent(
    NetEntity entity,
    NetEntity? used,
    Angle angle,
    TimeSpan frame,
    ProtoId<CEEntityEffectAnimationPrototype> animationId,
    float speed,
    NetEntity? targetEntity,
    NetCoordinates? targetCoordinates) : EntityEventArgs
{
    public NetEntity Entity = entity;
    public NetEntity? Used = used;
    public Angle Angle = angle;
    public TimeSpan Frame = frame;
    public ProtoId<CEEntityEffectAnimationPrototype> AnimationId = animationId;
    public float Speed = speed;
    public NetEntity? TargetEntity = targetEntity;
    public NetCoordinates? TargetCoordinates = targetCoordinates;
}


/// <summary>
/// Defines a keyframe for animating sprite offset (position relative to entity).
/// </summary>
[DataDefinition, Serializable, NetSerializable]
public sealed partial class CEOffsetKeyframe
{
    /// <summary>
    /// Time in seconds from the start of the animation when this keyframe is reached.
    /// </summary>
    [DataField(required: true)]
    public float Time;

    /// <summary>
    /// The sprite offset at this keyframe. X and Y are in world units.
    /// </summary>
    [DataField]
    public Vector2 Offset = Vector2.Zero;

    /// <summary>
    /// Easing function to use when transitioning to this keyframe.
    /// </summary>
    [DataField]
    public CEAnimationEasing Easing = CEAnimationEasing.Linear;
}

/// <summary>
/// Defines a keyframe for animating sprite rotation.
/// </summary>
[DataDefinition, Serializable, NetSerializable]
public sealed partial class CERotationKeyframe
{
    /// <summary>
    /// Time in seconds from the start of the animation when this keyframe is reached.
    /// </summary>
    [DataField(required: true)]
    public float Time;

    /// <summary>
    /// The sprite rotation at this keyframe in degrees.
    /// </summary>
    [DataField]
    public float Rotation;

    /// <summary>
    /// Easing function to use when transitioning to this keyframe.
    /// </summary>
    [DataField]
    public CEAnimationEasing Easing = CEAnimationEasing.Linear;
}

/// <summary>
/// Defines a keyframe for animating sprite color/alpha (for fade effects).
/// </summary>
[DataDefinition, Serializable, NetSerializable]
public sealed partial class CEColorKeyframe
{
    /// <summary>
    /// Time in seconds from the start of the animation when this keyframe is reached.
    /// </summary>
    [DataField(required: true)]
    public float Time;

    /// <summary>
    /// The color/alpha value at this keyframe. Use alpha channel for fade effects.
    /// </summary>
    [DataField]
    public Color Color = Color.White;

    /// <summary>
    /// Easing function to use when transitioning to this keyframe.
    /// </summary>
    [DataField]
    public CEAnimationEasing Easing = CEAnimationEasing.Linear;
}

/// <summary>
/// Defines a keyframe for animating sprite scale.
/// </summary>
[DataDefinition, Serializable, NetSerializable]
public sealed partial class CEScaleKeyFrame
{
    /// <summary>
    /// Time in seconds from the start of the animation when this keyframe is reached.
    /// </summary>
    [DataField(required: true)]
    public float Time;

    [DataField]
    public Vector2 Scale = Vector2.One;

    /// <summary>
    /// Easing function to use when transitioning to this keyframe.
    /// </summary>
    [DataField]
    public CEAnimationEasing Easing = CEAnimationEasing.Linear;
}

/// <summary>
/// Defines easing functions for animation interpolation.
/// </summary>
public enum CEAnimationEasing : byte
{
    Linear,
    QuadIn,
    QuadOut,
    QuadInOut,
    CubicIn,
    CubicOut,
    CubicInOut,
    QuartIn,
    QuartOut,
    QuartInOut,
}
