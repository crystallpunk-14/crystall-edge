using System.Linq;
using System.Numerics;
using Content.Shared._CE.Animation.Core.Components;
using Content.Shared._CE.Animation.Core.Prototypes;
using Content.Shared._CE.EntityEffect;
using Content.Shared._CE.EntityEffect.Effects;
using Content.Shared.Movement.Systems;
using JetBrains.Annotations;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared._CE.Animation.Core;

public abstract partial class CESharedAnimationActionSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private MovementSpeedModifierSystem _movement = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private IPrototypeManager _proto = default!;

    public override void Initialize()
    {
        base.Initialize();

        InitMovement();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<CEActiveAnimationActionComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var controller, out var xform))
        {
            if (!_proto.Resolve(controller.ActiveAnimation, out var animation))
            {
                StopAnimation((uid, controller));
                continue;
            }

            var speedMultiplier = 1f / controller.AnimationSpeed;

            var animationEndTime = controller.StartAnimationTime + (animation.Duration * speedMultiplier);

            //Finishing animation
            if (_timing.CurTime >= animationEndTime)
            {
                var finishedEv = new CEAnimationActionEndedEvent(animation, false);
                RaiseLocalEvent(uid, finishedEv);
                StopAnimation((uid, controller));
                continue;
            }

            if (_timing.ApplyingState)
                continue;

            // Rotate towards the target if LockRotation is active with a TargetEntity or TargetPosition.
            if (controller.LockRotation && controller.TargetEntity != uid)
            {
                Vector2? targetWorldPos = null;

                if (controller.TargetEntity.HasValue)
                    targetWorldPos = _transform.GetWorldPosition(Transform(controller.TargetEntity.Value));
                else if (controller.TargetCoordinates.HasValue)
                    targetWorldPos = _transform.ToMapCoordinates(controller.TargetCoordinates.Value).Position;

                if (targetWorldPos.HasValue)
                {
                    var myPos = _transform.GetWorldPosition(xform);
                    var diff = targetWorldPos.Value - myPos;
                    if (diff.LengthSquared() > 0.0001f)
                        _transform.SetWorldRotation(uid, diff.ToWorldAngle());
                }
            }

            //Processing animation events
            if (animation.Events.Any() && controller.StartAnimationTime.HasValue)
            {
                var effectArgs = new CEEntityEffectArgs(
                    EntityManager,
                    uid,
                    controller.Used,
                    _transform.GetWorldRotation(uid),
                    controller.AnimationSpeed,
                    controller.TargetEntity,
                    controller.TargetCoordinates);

                var startTime = controller.StartAnimationTime.Value;
                var anyEventFired = false;
                foreach (var (keyFrame, actions) in animation.Events)
                {
                    var realKeyFrame = keyFrame * speedMultiplier;
                    // Skip events already processed
                    if (realKeyFrame <= controller.LastEvent)
                        continue;

                    var eventTime = startTime + realKeyFrame;
                    // Only trigger if event time is within this frame
                    if (eventTime > _timing.CurTime)
                        continue;

                    foreach (var action in actions)
                    {
                        // Only fire game-logic and visual effects on entities this peer should simulate.
                        // On the client, only the locally-controlled player entity uses prediction;
                        // NPCs and other players receive visual effects via CEEntityAnimationEvent instead.
                        if (ShouldFireKeyframeEffects(uid))
                            action.Effect(effectArgs);
                    }

                    controller.LastEvent = realKeyFrame;
                    anyEventFired = true;
                    OnKeyframeActions(uid, controller, keyFrame, actions);
                }

                if (anyEventFired)
                    Dirty(uid, controller);
            }
        }
    }

    /// <summary>
    /// Starts an animation rotated by a specific angle.
    /// </summary>
    /// <param name="entity">The entity we animate</param>
    /// <param name="animationProto">Animation we play</param>
    /// <param name="angle">The angle at which the animation is rotated. If null, the character's current rotation angle is used.</param>
    /// <param name="used">The entity used for animation (object in hands?)</param>
    /// <param name="speed">The speed at which you need to start a new animation</param>
    /// <param name="forceCancel">Forcefully cancel the currently playing animation to start a new one</param>
    /// <returns></returns>
    [PublicAPI]
    public bool TryPlayAnimationToAngle(EntityUid entity,
        ProtoId<CEEntityEffectAnimationPrototype> animationProto,
        Angle? angle = null,
        EntityUid? used = null,
        float speed = 1f,
        bool forceCancel = false)
    {
        if (TryComp<CEActiveAnimationActionComponent>(entity, out var controller))
        {
            if (forceCancel)
                CancelAnimation((entity, controller));
            else
                return false;
        }

        if (!_proto.Resolve(animationProto, out var indexedAnimation))
            return false;

        StartAnimation(entity, indexedAnimation, used, angle ?? _transform.GetWorldRotation(entity), speed: speed, targetEntity: entity);
        return true;
    }

    /// <summary>
    /// Triggers an animation aimed at another entity.
    /// </summary>
    /// <param name="entity">The entity we animate</param>
    /// <param name="animationProto">Animation we play</param>
    /// <param name="target">The target entity we are aiming for</param>
    /// <param name="used">The entity used for animation (object in hands?)</param>
    /// <param name="speed">The speed at which you need to start a new animation</param>
    /// <param name="forceCancel">Forcefully cancel the currently playing animation to start a new one</param>
    /// <returns></returns>
    [PublicAPI]
    public bool TryPlayAnimationToEntity(EntityUid entity,
        ProtoId<CEEntityEffectAnimationPrototype> animationProto,
        EntityUid target,
        EntityUid? used = null,
        float speed = 1f,
        bool forceCancel = false)
    {
        if (TryComp<CEActiveAnimationActionComponent>(entity, out var controller))
        {
            if (forceCancel)
                CancelAnimation((entity, controller));
            else
                return false;
        }

        if (!_proto.Resolve(animationProto, out var indexedAnimation))
            return false;

        StartAnimation(entity, indexedAnimation, used, targetEntity: target, speed: speed);
        return true;
    }

    /// <summary>
    /// Triggers an animation aimed at specific coordinates in the world
    /// </summary>
    /// <param name="entity">The entity we animate</param>
    /// <param name="animationProto">Animation we play</param>
    /// <param name="target">The coordinates we are targeting.</param>
    /// <param name="used">The entity used for animation (object in hands?)</param>
    /// <param name="speed">The speed at which you need to start a new animation</param>
    /// <param name="forceCancel">Forcefully cancel the currently playing animation to start a new one</param>
    /// <returns></returns>
    [PublicAPI]
    public bool TryPlayAnimationToCoordinates(EntityUid entity,
        ProtoId<CEEntityEffectAnimationPrototype> animationProto,
        EntityCoordinates target,
        EntityUid? used = null,
        float speed = 1f,
        bool forceCancel = false)
    {
        if (TryComp<CEActiveAnimationActionComponent>(entity, out var controller))
        {
            if (forceCancel)
                CancelAnimation((entity, controller));
            else
                return false;
        }

        if (!_proto.Resolve(animationProto, out var indexedAnimation))
            return false;

        StartAnimation(entity, indexedAnimation, used, targetCoordinates: target, speed: speed);
        return true;
    }

    [PublicAPI]
    public bool IsPlayingAnimation(EntityUid entity)
    {
        return HasComp<CEActiveAnimationActionComponent>(entity);
    }

    /// <summary>
    /// Prematurely cancels animation execution
    /// </summary>
    [PublicAPI]
    public void CancelAnimation(Entity<CEActiveAnimationActionComponent> entity)
    {
        if (!_proto.Resolve(entity.Comp.ActiveAnimation, out var animation))
        {
            StopAnimation(entity);
            return;
        }

        //Canceling
        var cancelEv = new CEAnimationActionEndedEvent(animation, true);
        RaiseLocalEvent(entity, cancelEv);
        StopAnimation(entity);
    }

    /// <summary>
    /// Starts the specified animation, overwriting the current animations if they are playing.
    /// </summary>
    private void StartAnimation(
        EntityUid entity,
        CEEntityEffectAnimationPrototype animation,
        EntityUid? used = null,
        Angle? rotateTo = null,
        EntityUid? targetEntity = null,
        EntityCoordinates? targetCoordinates = null,
        float speed = 1f)
    {
        var controller = EnsureComp<CEActiveAnimationActionComponent>(entity);

        controller.ActiveAnimation = animation;
        controller.StartAnimationTime = _timing.CurTime;
        controller.LockRotation = animation.LockRotation;
        controller.TargetEntity = targetEntity;
        controller.TargetCoordinates = targetCoordinates;
        controller.Used = used;
        controller.AnimationSpeed = speed;
        Dirty(entity, controller);

        // Face the animation direction before locking rotation.
        // For players this points toward the mouse; for NPCs toward the target.
        if (animation.LockRotation && rotateTo != null)
            _transform.SetWorldRotation(entity, rotateTo.Value);

        var started = new CEAnimationActionStartedEvent(animation);
        RaiseLocalEvent(entity, started);

        _movement.RefreshMovementSpeedModifiers(entity);
    }

    /// <summary>
    /// Stops any animation being played on an entity
    /// </summary>
    /// <param name="entity"></param>
    private void StopAnimation(Entity<CEActiveAnimationActionComponent> entity)
    {
        RemComp<CEActiveAnimationActionComponent>(entity);
        _movement.RefreshMovementSpeedModifiers(entity);
    }

    /// <summary>
    /// Called server-side after a keyframe's actions have been executed.
    /// Override to send network events (e.g. <see cref="CEEntityAnimationEvent"/>) to non-predicting clients.
    /// </summary>
    protected virtual void OnKeyframeActions(EntityUid uid, CEActiveAnimationActionComponent controller, TimeSpan keyFrame, List<CEEntityEffect> actions)
    {
    }

    /// <summary>
    /// Returns <c>true</c> if keyframe effects should be fired for <paramref name="uid"/> on this peer.
    /// The server always returns <c>true</c>.
    /// The client override returns <c>true</c> only for the locally-controlled player entity so that
    /// NPC and other-player animations don't double-fire: they receive visual effects via
    /// <see cref="CEEntityAnimationEvent"/> from the server instead.
    /// </summary>
    protected virtual bool ShouldFireKeyframeEffects(EntityUid uid) => true;
}

/// <summary>
/// TODO
/// </summary>
/// <param name="animation"></param>
/// <param name="cancelled"></param>
public sealed partial class CEAnimationActionEndedEvent(ProtoId<CEEntityEffectAnimationPrototype> animation, bool cancelled)
    : EntityEventArgs
{
    public ProtoId<CEEntityEffectAnimationPrototype> Animation = animation;
    public bool Cancelled = cancelled;
}

/// <summary>
/// TODO
/// </summary>
/// <param name="animation"></param>
public sealed partial class CEAnimationActionStartedEvent(ProtoId<CEEntityEffectAnimationPrototype> animation) : EntityEventArgs
{
    public ProtoId<CEEntityEffectAnimationPrototype> Animation = animation;
}
