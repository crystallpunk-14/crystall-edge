using Content.Shared.EntityEffects;
using Content.Shared.Power.Components;
using Content.Shared.Power.EntitySystems;

namespace Content.Shared._White.EntityEffects.Effects;

public sealed partial class WhiteChargeChangeEntityEffectSystem : EntityEffectSystem<BatteryComponent, WhiteChargeChange>
{
    [Dependency] private readonly SharedBatterySystem _battery = default!;

    protected override void Effect(Entity<BatteryComponent> entity, ref EntityEffectEvent<WhiteChargeChange> args)
    {
        _battery.ChangeCharge(entity.AsNullable(), args.Effect.ChargeDelta * args.Scale, args.Effect.Safe);
    }
}

public sealed partial class WhiteChargeChange : EntityEffectBase<WhiteChargeChange>
{
    [DataField(required: true)]
    public float ChargeDelta = 1;

    [DataField]
    public bool Safe;
}
