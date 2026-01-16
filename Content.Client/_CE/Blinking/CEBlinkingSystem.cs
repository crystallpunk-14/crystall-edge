/*
 * This file is sublicensed under MIT License
 * https://github.com/space-wizards/space-station-14/blob/master/LICENSE.TXT
 *
 * Taken from https://github.com/EphemeralSpace/ephemeral-space/pull/335/files?notification_referrer_id=NT_kwDOBb-lNbQyMDgzMjQ4Nzk4Nzo5NjQ0NTc0OQ
 */

using Content.Shared._CE.Blinking;
using Content.Shared.Humanoid;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;

namespace Content.Client._CE.Blinking;

/// <inheritdoc/>
public sealed class CEBlinkingSystem : CESharedBlinkingSystem
{
    [Dependency] private readonly AnimationPlayerSystem _animationPlayer = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    private const string AnimationKey = "anim-blink";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CEBlinkerComponent, AppearanceChangeEvent>(OnAppearanceChange);
    }

    private void OnAppearanceChange(Entity<CEBlinkerComponent> ent, ref AppearanceChangeEvent args)
    {
        if (!Appearance.TryGetData<bool>(ent.Owner, CEBlinkVisuals.EyesClosed, out var closed))
            return;

        if (!_sprite.LayerMapTryGet(ent.Owner, HumanoidVisualLayers.Eyes, out var idx, false))
            return;

        _sprite.LayerSetVisible(ent.Owner, idx, !closed);
    }

    public override void Blink(Entity<CEBlinkerComponent> ent)
    {
        base.Blink(ent);

        if (_animationPlayer.HasRunningAnimation(ent.Owner, AnimationKey))
            return;

        if (!_sprite.TryGetLayer(ent.Owner, HumanoidVisualLayers.Eyes, out var layer, false))
            return;

        var animation = new Animation
        {
            Length = TimeSpan.FromSeconds(0.5f),
            AnimationTracks =
            {
                new AnimationTrackSpriteFlick
                {
                    LayerKey = HumanoidVisualLayers.Eyes,
                    KeyFrames =
                    {
                        new AnimationTrackSpriteFlick.KeyFrame(new RSI.StateId("no_eyes"), 0f),
                        new AnimationTrackSpriteFlick.KeyFrame(layer.State, 0.10f),
                    }
                }
            },
        };

        _animationPlayer.Play(ent.Owner, animation, AnimationKey);
    }
}
