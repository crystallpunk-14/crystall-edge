using Content.Shared._CE.Pinpointer;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Client._CE.Pinpointer;

public sealed partial class CEPinpointerSystem : EntitySystem
{
    [Dependency] private IEyeManager _eye = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SpriteSystem _sprite = default!;

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        var now = _timing.RealTime;
        var eyeRotation = _eye.CurrentEye.Rotation;

        var query = EntityQueryEnumerator<CEPinpointerComponent, SpriteComponent>();
        while (query.MoveNext(out var uid, out var pinpointer, out var sprite))
        {
            if (pinpointer.HasDirection)
                Point(pinpointer, pinpointer.ArrowAngle + eyeRotation, frameTime);
            else
                Wander(pinpointer, now, frameTime);

            var rotation = pinpointer.NeedleAngle;

            // Rotate around the pivot instead of the texture center
            var offset = pinpointer.NeedlePivot - rotation.RotateVec(pinpointer.NeedlePivot);

            _sprite.LayerSetRotation((uid, sprite), CEPinpointerLayers.Needle, rotation);
            _sprite.LayerSetOffset((uid, sprite), CEPinpointerLayers.Needle, offset);
        }
    }

    private static void Point(CEPinpointerComponent pinpointer, Angle goal, float frameTime)
    {
        pinpointer.WanderRemaining = 0f;

        var factor = MathF.Min(1f, pinpointer.NeedleStiffness * frameTime);
        pinpointer.NeedleAngle = Angle.Lerp(pinpointer.NeedleAngle, goal, factor).Reduced();
    }

    private void Wander(CEPinpointerComponent pinpointer, TimeSpan now, float frameTime)
    {
        if (now >= pinpointer.NextWander)
        {
            var swing = _random.NextFloat(0.3f, pinpointer.WanderMaxSwing);
            pinpointer.WanderRemaining = _random.Prob(0.5f) ? swing : -swing;
            pinpointer.WanderSpeed = _random.NextFloat(pinpointer.WanderSpeedMin, pinpointer.WanderSpeedMax);

            var swingTime = swing / pinpointer.WanderSpeed + _random.NextFloat(0f, pinpointer.WanderMaxPause);
            pinpointer.NextWander = now + TimeSpan.FromSeconds(swingTime);
        }

        if (pinpointer.WanderRemaining == 0f)
            return;

        var step = MathF.Min(MathF.Abs(pinpointer.WanderRemaining), pinpointer.WanderSpeed * frameTime);
        step *= MathF.Sign(pinpointer.WanderRemaining);

        pinpointer.WanderRemaining -= step;
        pinpointer.NeedleAngle = (pinpointer.NeedleAngle + step).Reduced();
    }
}
