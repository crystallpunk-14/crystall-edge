using System.Numerics;
using Content.Shared._CE.InteractionParticle;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using DrawDepth = Content.Shared.DrawDepth.DrawDepth;

namespace Content.Client._CE.InteractionParticle;

/// <summary>
/// Pops a copy of the item's sprite up above the performer's head and fades it out.
/// Based on the interaction particles by Janet Blackquill (Stellar Station).
/// </summary>
public sealed partial class CEClientInteractionParticleSystem : EntitySystem
{
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private AnimationPlayerSystem _animation = default!;
    [Dependency] private AppearanceSystem _appearance = default!;
    [Dependency] private SpriteSystem _sprite = default!;

    private static readonly EntProtoId ParticleProto = "CEInteractionParticle";

    private const string AnimationKey = "ce-interaction-particle";

    private static readonly Vector2 StartScale = new(0.3f, 0.3f);
    private static readonly Vector2 RiseOffset = new(0f, 0.85f);
    private const float MaxStartRotation = 40f;
    private const float StartAlpha = 0.7f;
    private static readonly TimeSpan GrowLength = TimeSpan.FromMilliseconds(600);
    private static readonly TimeSpan RiseLength = TimeSpan.FromMilliseconds(250);

    [EventSubscription]
    private void OnInteractionParticle(CEInteractionParticleEvent ev)
    {
        var performer = GetEntity(ev.Performer);
        var item = GetEntity(ev.Item);

        if (!Exists(performer) || !TryComp<SpriteComponent>(item, out var itemSprite))
            return;

        if (Transform(performer).MapID == MapId.Nullspace)
            return;

        var particle = Spawn(ParticleProto, new EntityCoordinates(performer, Vector2.Zero));

        // The item may have changed appearance this tick, refresh it before copying.
        _appearance.OnChangeData(item, itemSprite);
        _sprite.CopySprite((item, itemSprite), particle);
        _sprite.SetDrawDepth(particle, (int) DrawDepth.Effects);

        var sprite = Comp<SpriteComponent>(particle);
        sprite.NoRotation = true;
        _sprite.SetVisible((particle, sprite), true);

        _animation.Play(particle, GetAnimation(sprite.Color), AnimationKey);
    }

    private Robust.Client.Animations.Animation GetAnimation(Color color)
    {
        var startRotation = _random.NextAngle(Angle.FromDegrees(-MaxStartRotation), Angle.FromDegrees(MaxStartRotation));
        var startColor = color.WithAlpha(color.A * StartAlpha);
        var endColor = color.WithAlpha(0f);

        return new Robust.Client.Animations.Animation
        {
            Length = GrowLength + RiseLength,
            AnimationTracks =
            {
                new AnimationTrackComponentProperty
                {
                    ComponentType = typeof(SpriteComponent),
                    Property = nameof(SpriteComponent.Rotation),
                    KeyFrames =
                    {
                        new AnimationTrackProperty.KeyFrame(startRotation, 0f),
                        new AnimationTrackProperty.KeyFrame(Angle.Zero, (float) GrowLength.TotalSeconds, Easings.OutBack),
                    },
                },
                new AnimationTrackComponentProperty
                {
                    ComponentType = typeof(SpriteComponent),
                    Property = nameof(SpriteComponent.Scale),
                    KeyFrames =
                    {
                        new AnimationTrackProperty.KeyFrame(StartScale, 0f),
                        new AnimationTrackProperty.KeyFrame(Vector2.One, (float) GrowLength.TotalSeconds, Easings.OutBack),
                    },
                },
                new AnimationTrackComponentProperty
                {
                    ComponentType = typeof(SpriteComponent),
                    Property = nameof(SpriteComponent.Color),
                    KeyFrames =
                    {
                        new AnimationTrackProperty.KeyFrame(startColor, 0f),
                        new AnimationTrackProperty.KeyFrame(startColor, (float) GrowLength.TotalSeconds),
                        new AnimationTrackProperty.KeyFrame(endColor, (float) RiseLength.TotalSeconds, Easings.InOutCirc),
                    },
                },
                new AnimationTrackComponentProperty
                {
                    ComponentType = typeof(SpriteComponent),
                    Property = nameof(SpriteComponent.Offset),
                    KeyFrames =
                    {
                        new AnimationTrackProperty.KeyFrame(Vector2.Zero, 0f),
                        new AnimationTrackProperty.KeyFrame(RiseOffset, (float) RiseLength.TotalSeconds, Easings.OutBack),
                    },
                },
            },
        };
    }
}
