using Content.Shared._CE.StatusEffect.BatteryRecharge;
using Content.Shared.Mobs.Systems;
using Content.Shared.Power.Components;
using Content.Shared.Power.EntitySystems;
using Content.Shared.StatusEffectNew.Components;

namespace Content.Server._CE.StatusEffect.BatteryRecharge;

public sealed partial class CEBatteryRechargeStatusEffectSystem : EntitySystem
{
    /// <summary>
    /// Margin kept from the battery limits so float rounding never trips the overcharge / deficit events.
    /// </summary>
    private const float LimitMargin = 0.001f;

    [Dependency] private SharedBatterySystem _battery = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private EntityQuery<BatteryComponent> _batteryQuery = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<CEBatteryRechargeStatusEffectComponent, StatusEffectComponent>();
        while (query.MoveNext(out _, out var recharge, out var status))
        {
            if (status.AppliedTo is not { } target)
                continue;

            if (!_batteryQuery.TryComp(target, out var battery))
                continue;

            if (_mobState.IsDead(target)) // Meh dont like it here
                continue;

            var charge = _battery.GetCharge((target, battery));
            var delta = recharge.Rate * frameTime;

            delta = delta > 0
                ? MathF.Min(delta, MathF.Max(0f, battery.MaxCharge - charge - LimitMargin))
                : MathF.Max(delta, -MathF.Max(0f, charge - LimitMargin));

            if (delta == 0f)
                continue;

            _battery.ChangeCharge((target, battery), delta);
        }
    }
}
