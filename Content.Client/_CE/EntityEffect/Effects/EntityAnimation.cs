using System.Numerics;
using Content.Client.Animations;
using Content.Shared._CE.EntityEffect;
using Content.Shared._CE.EntityEffect.Effects;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Robust.Client.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Spawners;
using Robust.Shared.Timing;

namespace Content.Client._CE.EntityEffect.Effects;

public sealed partial class CEEntityAnimationEffectSystem : CEEntityEffectSystem<EntityAnimation>
{
    private const string OffsetAnimationKey = "ce-item-visual-offset";
    private const string RotationAnimationKey = "ce-item-visual-rotation";
    private const string ColorAnimationKey = "ce-item-visual-color";
    private const string ScaleAnimationKey = "ce-item-visual-scale";

    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IPrototypeManager _protoManager = default!;
    [Dependency] private TransformSystem _transform = default!;
    [Dependency] private SpriteSystem _sprite = default!;
    [Dependency] private AnimationPlayerSystem _animationPlayer = default!;
    [Dependency] private SharedHandsSystem _hands = default!;

    [Dependency] private EntityQuery<HandsComponent> _handsQuery = default!;
    [Dependency] private EntityQuery<SpriteComponent> _spriteQuery = default!;

    protected override void Effect(ref CEEntityEffectEvent<EntityAnimation> args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        if (ResolveEffectEntity(args.Args, args.Effect.EffectTarget) is not { } entity)
            return;

        var effect = args.Effect;
        var angle = args.Args.Angle;
        var speedMultiplier = 1f / args.Args.Speed;

        var entityXform = Transform(entity);

        if (entityXform.MapID == MapId.Nullspace)
            return;

        // Spawn a client-side clone entity at the entity's position
        var effectEntity = Spawn("clientsideclone", entityXform.Coordinates);

        if (!_spriteQuery.TryComp(effectEntity, out var effectSprite))
            return;

        // Resolve sprite source (in priority order):
        // 1. DummyEntity — explicit VFX entity prototype, always wins
        // 2. ActiveHandEntityAnimation — resolve from user's active hand
        // 3. OffHandEntityAnimation — resolve from user's non-active hand
        // 4. UsedEntityAnimation — use the action's container item (args.Used)
        EntityUid? spriteSource = null;
        if (effect.DummyEntity != null)
        {
            var dummy = Spawn(effect.DummyEntity);
            if (_spriteQuery.TryComp(dummy, out var dummySprite))
                _sprite.CopySprite((dummy, dummySprite), (effectEntity, effectSprite));
            Del(dummy);

            var proto = _protoManager.Index(effect.DummyEntity.Value);
            EntityManager.AddComponents(effectEntity, proto, false);
        }
        else if (effect is ActiveHandEntityAnimation
                 && _hands.TryGetActiveItem(entity, out var activeItem)
                 && _spriteQuery.TryComp(activeItem.Value, out var handSprite))
        {
            spriteSource = activeItem.Value;
            _sprite.CopySprite((activeItem.Value, handSprite), (effectEntity, effectSprite));
        }
        else if (effect is OffHandEntityAnimation
                 && _handsQuery.TryComp(entity, out var handsComp))
        {
            foreach (var handId in handsComp.SortedHands)
            {
                if (handId == handsComp.ActiveHandId) continue;
                if (!_hands.TryGetHeldItem(entity, handId, out var offHandItem)) continue;
                if (!_spriteQuery.TryComp(offHandItem.Value, out var offHandSprite)) continue;
                spriteSource = offHandItem.Value;
                _sprite.CopySprite((offHandItem.Value, offHandSprite), (effectEntity, effectSprite));
                break;
            }
        }
        else if (args.Args.Used is { } used
                 && _spriteQuery.TryComp(used, out var itemSprite))
        {
            spriteSource = used;
            _sprite.CopySprite((used, itemSprite), (effectEntity, effectSprite));
        }

        _sprite.SetVisible((effectEntity, effectSprite), true);
        _sprite.SetDrawDepth((effectEntity, effectSprite), (int)Shared.DrawDepth.DrawDepth.Effects);

        // Set initial rotation
        var initialRotation = angle;
        //if (TryComp<CEWeaponComponent>(spriteSource, out var itemAnim))
        //    initialRotation += Angle.FromDegrees(itemAnim.SpriteRotation);

        _sprite.SetRotation((effectEntity, effectSprite), initialRotation);

        // Get initial offset from first keyframe or use zero
        var initialOffset = Vector2.Zero;
        if (effect.OffsetAnimation.Count > 0)
        {
            var firstKeyframe = effect.OffsetAnimation[0];
            if (firstKeyframe.Time == 0)
                initialOffset = firstKeyframe.Offset;
        }

        // Set up to follow the entity if enabled
        if (effect.FollowUser)
        {
            var track = EnsureComp<TrackUserComponent>(effectEntity);
            track.User = entity;
        }
        else
        {
            // Position at the offset from the entity if not following
            var worldPos = _transform.GetWorldPosition(entityXform) + angle.RotateVec(initialOffset);
            _transform.SetWorldPosition(effectEntity, worldPos);
        }

        // Set up timed despawn
        var despawn = EnsureComp<TimedDespawnComponent>(effectEntity);
        despawn.Lifetime = CEAnimationTrackBuilders.CalculateDuration(effect, speedMultiplier) + 0.1f;

        // Build and play offset animation if keyframes exist
        if (effect.OffsetAnimation.Count > 0)
        {
            var offsetAnim = CEAnimationTrackBuilders.BuildOffsetAnimation(effect.OffsetAnimation, speedMultiplier, angle);
            _animationPlayer.Play(effectEntity, offsetAnim, OffsetAnimationKey);
        }

        // Build and play rotation animation if keyframes exist
        if (effect.RotationAnimation.Count > 0)
        {
            var rotationAnim = CEAnimationTrackBuilders.BuildRotationAnimation(effect.RotationAnimation, speedMultiplier, initialRotation);
            _animationPlayer.Play(effectEntity, rotationAnim, RotationAnimationKey);
        }

        // Build and play color animation if keyframes exist
        if (effect.ColorAnimation.Count > 0)
        {
            var colorAnim = CEAnimationTrackBuilders.BuildColorAnimation(effect.ColorAnimation, speedMultiplier);
            _animationPlayer.Play(effectEntity, colorAnim, ColorAnimationKey);
        }

        // Build and play scale animation if keyframes exist
        if (effect.ScaleAnimation.Count > 0)
        {
            var scaleAnim = CEAnimationTrackBuilders.BuildScaleAnimation(effect.ScaleAnimation, speedMultiplier);
            _animationPlayer.Play(effectEntity, scaleAnim, ScaleAnimationKey);
        }
    }
}
