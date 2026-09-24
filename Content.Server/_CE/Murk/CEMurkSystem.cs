using Content.Server.Power.Components;
using Content.Shared._CE.Murk;
using Content.Shared._CE.Murk.Components;

namespace Content.Server._CE.Murk;

public sealed partial class CEMurkSystem : CESharedMurkSystem
{
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<CEMurkGeneratorComponent, CEMurkSourceComponent, ApcPowerReceiverComponent>();
        while (query.MoveNext(out var uid, out var generator, out var source, out var receiver))
        {
            var target = receiver.Powered ? generator.EnabledIntensity : generator.DisabledIntensity;

            var remaining = target - source.Intensity;
            if (remaining == 0f)
                continue;

            var step = generator.ChangeRate * frameTime;
            source.Intensity += Math.Clamp(remaining, -step, step);
            Dirty(uid, source);
        }
    }
}
