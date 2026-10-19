using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.StatusEffectNew;
using Content.Shared.StatusEffectNew.Components;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared._CE.StatusEffect.MysticVitality;

public sealed partial class CEMysticVitalityStatusEffectSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private MobThresholdSystem _mobThreshold = default!;

    [SubscribeLocalEvent]
    private void OnDamageModify(Entity<CEMysticVitalityStatusEffectComponent> ent, ref StatusEffectRelayedEvent<DamageModifyEvent> args)
    {
        var target = args.AppliedTo;
        if (_mobState.IsDead(target) ||
            !TryComp<DamageableComponent>(target, out var damageable) ||
            !_mobThreshold.TryGetThresholdForState(target, MobState.Dead, out var deadThreshold))
        {
            return;
        }

        var incoming = FixedPoint2.Zero;
        var healing = FixedPoint2.Zero;

        foreach (var (_, value) in args.Args.Damage.DamageDict)
        {
            if (value <= 0)
                healing += value;
            else
                incoming += value;
        }

        var allowed = FixedPoint2.Max(FixedPoint2.Zero,
            deadThreshold.Value - ent.Comp.DeathMargin - _damageable.GetTotalDamage((target, damageable)) - healing);
        var scale = incoming > allowed ? allowed.Float() / incoming.Float() : 1f;

        var damage = new DamageSpecifier();
        foreach (var (type, value) in args.Args.Damage.DamageDict)
        {
            damage.DamageDict[type] = value <= 0 ? value : value * scale;
        }

        args.Args.Damage = damage;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_net.IsClient)
            return;

        var query = EntityQueryEnumerator<CEMysticVitalityStatusEffectComponent, StatusEffectComponent>();
        while (query.MoveNext(out var uid, out var vitality, out var status))
        {
            if (_timing.CurTime < vitality.NextRegen)
                continue;

            vitality.NextRegen = _timing.CurTime + vitality.RegenInterval;

            if (status.AppliedTo is not { } target ||
                !_mobState.IsCritical(target) ||
                !TryComp<DamageableComponent>(target, out var damageable))
            {
                continue;
            }

            var total = _damageable.GetTotalDamage((target, damageable)).Float();
            if (total <= 0)
                continue;

            var fraction = MathF.Min(vitality.RegenAmount.Float(), total) / total;
            _damageable.TryChangeDamage((target, damageable),
                -(_damageable.GetAllDamage((target, damageable)) * fraction),
                ignoreResistances: true,
                interruptsDoAfters: false);
        }
    }
}
