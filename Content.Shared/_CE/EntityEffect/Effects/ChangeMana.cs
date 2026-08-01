using Content.Shared.Power.Components;
using Content.Shared.Power.EntitySystems;

namespace Content.Shared._CE.EntityEffect.Effects;

/// <summary>
/// Changes the target's battery charge. Positive <see cref="Amount"/> restores mana, negative drains it.
/// </summary>
public sealed partial class ChangeMana : CEEntityEffectBase<ChangeMana>
{
    [DataField]
    public int Amount = 1;
}

public sealed partial class CEChangeManaEffectSystem : CEEntityEffectSystem<ChangeMana>
{
    [Dependency] private SharedBatterySystem _battery = default!;

    protected override void Effect(ref CEEntityEffectEvent<ChangeMana> args)
    {
        if (ResolveEffectEntity(args.Args, args.Effect.EffectTarget) is not { } entity)
            return;

        // SharedBatterySystem.ChangeCharge logs an error if the target has no BatteryComponent - this
        // effect is applied broadly (e.g. via AreaEffect to everything nearby), so that's expected here.
        if (!TryComp<BatteryComponent>(entity, out var battery))
            return;

        _battery.ChangeCharge((entity, battery), args.Effect.Amount * args.Args.Power);
    }
}
