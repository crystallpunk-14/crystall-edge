using Content.Shared._CE.EntityEffect;
using Content.Shared._CE.EntityEffect.Effects;
using Content.Shared._CE.Power;

namespace Content.Server._CE.EntityEffect.Effects;

public sealed partial class CEIrradiateEffectSystem : CEEntityEffectSystem<Irradiate>
{
    [Dependency] private CESharedPowerSystem _power = default!;

    protected override void Effect(ref CEEntityEffectEvent<Irradiate> args)
    {
        if (!TryResolveEffectCoordinates(args.Args, args.Effect.EffectTarget, out var targetPoint))
            return;

        _power.Irradiate(targetPoint, args.Effect.Charge, args.Effect.Duration);
    }
}
