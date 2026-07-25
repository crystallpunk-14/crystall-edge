using System.Numerics;
using Content.Shared._CE.Press.Components;
using Content.Shared._CE.Press.Systems;
using Robust.Client.GameObjects;
using Robust.Shared.Timing;

namespace Content.Client._CE.Press.Systems;

/// <summary>
/// Client-side visuals for <see cref="CEPressComponent"/>: animates the "block" sprite layer's
/// vertical offset to follow the press's Idle/Preparing/Crushing/Recovering cycle.
/// </summary>
public sealed partial class CEPressSystem : CESharedPressSystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SpriteSystem _sprite = default!;

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        var query = EntityQueryEnumerator<CEPressComponent, SpriteComponent>();
        while (query.MoveNext(out var uid, out var press, out var sprite))
        {
            var offsetY = press.State switch
            {
                CEPressState.Preparing => GetPreparingOffset(press),
                CEPressState.Crushing => press.CrushOffset,
                CEPressState.Recovering => float.Lerp(press.CrushOffset, press.DefaultOffset, QuadOut(GetProgress(press, press.RecoveringDuration))),
                _ => press.DefaultOffset,
            };

            _sprite.LayerSetOffset((uid, sprite), press.BlockLayerKey, new Vector2(0, offsetY));
        }
    }

    /// <summary>
    /// Preparing rises (QuadOut) from DefaultOffset to PreparingOffset, then over the last
    /// <see cref="CEPressComponent.FallDuration"/> of Preparing falls (QuadIn) to CrushOffset,
    /// timed so the fall finishes exactly when Preparing ends and crushing occurs.
    /// </summary>
    private float GetPreparingOffset(CEPressComponent press)
    {
        var fallDuration = press.FallDuration > press.PreparingDuration ? press.PreparingDuration : press.FallDuration;
        var riseDuration = press.PreparingDuration - fallDuration;

        var remaining = (press.StateEndTime - _timing.CurTime).TotalSeconds;
        var elapsed = press.PreparingDuration.TotalSeconds - remaining;

        if (riseDuration <= TimeSpan.Zero || elapsed < riseDuration.TotalSeconds)
        {
            var riseProgress = riseDuration <= TimeSpan.Zero
                ? 1f
                : float.Clamp((float) (elapsed / riseDuration.TotalSeconds), 0f, 1f);
            return float.Lerp(press.DefaultOffset, press.PreparingOffset, QuadOut(riseProgress));
        }

        var fallProgress = fallDuration <= TimeSpan.Zero
            ? 1f
            : float.Clamp((float) ((elapsed - riseDuration.TotalSeconds) / fallDuration.TotalSeconds), 0f, 1f);
        return float.Lerp(press.PreparingOffset, press.CrushOffset, QuadIn(fallProgress));
    }

    private float GetProgress(CEPressComponent press, TimeSpan duration)
    {
        if (duration <= TimeSpan.Zero)
            return 1f;

        var remaining = (press.StateEndTime - _timing.CurTime).TotalSeconds;
        var elapsed = duration.TotalSeconds - remaining;
        return float.Clamp((float) (elapsed / duration.TotalSeconds), 0f, 1f);
    }

    private static float QuadIn(float t) => t * t;

    private static float QuadOut(float t) => 1f - (1f - t) * (1f - t);
}
