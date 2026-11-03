using System.Numerics;
using Content.Shared._CE.Workbench.Press;
using Robust.Client.GameObjects;
using Robust.Shared.Timing;

namespace Content.Client._CE.Workbench.Press;

/// <summary>
/// Animates the "block" sprite layer of <see cref="CEPressVisualsComponent"/> entities following the
/// autocrafter cycle: rise while crafting, slam down on completion, then recover.
/// </summary>
public sealed partial class CEPressVisualsSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SpriteSystem _sprite = default!;

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        var curTime = _timing.CurTime;
        var query = EntityQueryEnumerator<CEPressVisualsComponent, SpriteComponent>();
        while (query.MoveNext(out var uid, out var press, out var sprite))
        {
            float offsetY;
            if (press.CraftEndTime != TimeSpan.Zero)
            {
                // Past the expected end the block stays down until the server confirms the craft
                offsetY = curTime < press.CraftEndTime
                    ? GetPreparingOffset(press, curTime)
                    : press.CrushOffset;
            }
            else if (press.LastCrushTime != TimeSpan.Zero && curTime < press.LastCrushTime + press.RecoveringDuration)
            {
                offsetY = GetRecoveringOffset(press, curTime);
            }
            else
            {
                offsetY = GetIdleOffset(uid, press, sprite, frameTime);
            }

            _sprite.LayerSetOffset((uid, sprite), press.BlockLayerKey, new Vector2(0, offsetY));
        }
    }

    /// <summary>
    /// Eases the block toward DefaultOffset with frame-rate-independent exponential decay,
    /// so an interrupted craft doesn't snap the block into place.
    /// </summary>
    private float GetIdleOffset(EntityUid uid, CEPressVisualsComponent press, SpriteComponent sprite, float frameTime)
    {
        if (!_sprite.TryGetLayer((uid, sprite), press.BlockLayerKey, out var layer, false))
            return press.DefaultOffset;

        var halfLife = MathF.Max(press.IdleEaseHalfLife, 0.001f);
        var t = 1f - MathF.Pow(0.5f, frameTime / halfLife);
        return float.Lerp(layer.Offset.Y, press.DefaultOffset, t);
    }

    /// <summary>
    /// Rises (SmoothStep) from DefaultOffset to PreparingOffset, then over the last
    /// <see cref="CEPressVisualsComponent.FallDuration"/> of the craft falls (QuadIn) to CrushOffset,
    /// so the impact lands exactly when the craft completes.
    /// </summary>
    private float GetPreparingOffset(CEPressVisualsComponent press, TimeSpan curTime)
    {
        var duration = press.CraftEndTime - press.CraftStartTime;
        var fallDuration = press.FallDuration > duration ? duration : press.FallDuration;
        var riseDuration = duration - fallDuration;
        var elapsed = (curTime - press.CraftStartTime).TotalSeconds;

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
    /// Holds at CrushOffset for <see cref="CEPressVisualsComponent.HoldDuration"/>, then rises
    /// (SmoothStep) back to DefaultOffset for the rest of the recovery.
    /// </summary>
    private float GetRecoveringOffset(CEPressVisualsComponent press, TimeSpan curTime)
    {
        var holdDuration = press.HoldDuration > press.RecoveringDuration ? press.RecoveringDuration : press.HoldDuration;
        var riseDuration = press.RecoveringDuration - holdDuration;
        var elapsed = (curTime - press.LastCrushTime).TotalSeconds;

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
