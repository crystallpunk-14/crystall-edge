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
                CEPressState.Recovering => GetRecoveringOffset(press),
                _ => GetIdleOffset(uid, press, sprite, frameTime),
            };

            _sprite.LayerSetOffset((uid, sprite), press.BlockLayerKey, new Vector2(0, offsetY));
        }
    }

    /// <summary>
    /// While Idle, eases the "block" layer's current offset toward DefaultOffset using
    /// frame-rate-independent exponential decay, rather than snapping there instantly (e.g. right
    /// after power is cut mid-cycle, wherever the block currently was).
    /// </summary>
    private float GetIdleOffset(EntityUid uid, CEPressComponent press, SpriteComponent sprite, float frameTime)
    {
        if (!_sprite.TryGetLayer((uid, sprite), press.BlockLayerKey, out var layer, false))
            return press.DefaultOffset;

        var current = layer.Offset.Y;
        var halfLife = MathF.Max(press.IdleEaseHalfLife, 0.001f);
        var t = 1f - MathF.Pow(0.5f, frameTime / halfLife);
        return float.Lerp(current, press.DefaultOffset, t);
    }

    /// <summary>
    /// Preparing rises (SmoothStep, zero velocity at both ends) from DefaultOffset to
    /// PreparingOffset, then over the last <see cref="CEPressComponent.FallDuration"/> of
    /// Preparing falls (QuadIn) to CrushOffset, timed so the fall finishes exactly when Preparing
    /// ends and crushing occurs. SmoothStep is used for the rise (rather than QuadOut) so its
    /// start velocity matches Recovering's rise ending at zero, avoiding a jerk at that seam.
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
            return float.Lerp(press.DefaultOffset, press.PreparingOffset, SmoothStep(riseProgress));
        }

        var fallProgress = fallDuration <= TimeSpan.Zero
            ? 1f
            : float.Clamp((float) ((elapsed - riseDuration.TotalSeconds) / fallDuration.TotalSeconds), 0f, 1f);
        return float.Lerp(press.PreparingOffset, press.CrushOffset, QuadIn(fallProgress));
    }

    /// <summary>
    /// Recovering holds at CrushOffset for <see cref="CEPressComponent.HoldDuration"/>, then rises
    /// (SmoothStep) to DefaultOffset for the remainder of Recovering. SmoothStep has zero velocity
    /// at both ends: zero at the start (matching the Hold's stationary end, avoiding the jerk right
    /// after impact) and zero at the end (matching Preparing's rise starting at zero, avoiding a
    /// jerk at the Recovering-to-Preparing seam).
    /// </summary>
    private float GetRecoveringOffset(CEPressComponent press)
    {
        var holdDuration = press.HoldDuration > press.RecoveringDuration ? press.RecoveringDuration : press.HoldDuration;
        var riseDuration = press.RecoveringDuration - holdDuration;

        var remaining = (press.StateEndTime - _timing.CurTime).TotalSeconds;
        var elapsed = press.RecoveringDuration.TotalSeconds - remaining;

        if (elapsed < holdDuration.TotalSeconds)
            return press.CrushOffset;

        var riseProgress = riseDuration <= TimeSpan.Zero
            ? 1f
            : float.Clamp((float) ((elapsed - holdDuration.TotalSeconds) / riseDuration.TotalSeconds), 0f, 1f);
        return float.Lerp(press.CrushOffset, press.DefaultOffset, SmoothStep(riseProgress));
    }

    private static float QuadIn(float t)
    {
        return t * t;
    }

    private static float SmoothStep(float t)
    {
        return t * t * (3f - 2f * t);
    }
}
