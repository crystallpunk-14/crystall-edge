using Content.Shared.Power.EntitySystems;

namespace Content.Shared._CE.EntityEffect.Effects;

public sealed partial class RestoreMana : CEEntityEffectBase<RestoreMana>
{
    [DataField]
    public int Amount = 1;
}

public sealed partial class CERestoreManaEffectSystem : CEEntityEffectSystem<RestoreMana>
{
    [Dependency] private SharedBatterySystem _battery = default!;

    protected override void Effect(ref CEEntityEffectEvent<RestoreMana> args)
    {
        if (ResolveEffectEntity(args.Args, args.Effect.EffectTarget) is not { } entity)
            return;

        _battery.ChangeCharge(entity, args.Effect.Amount * args.Args.Power);
    }
}
