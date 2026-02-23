using Content.Shared._White.StatusEffect.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.StatusEffectNew;

namespace Content.Shared._White.StatusEffect.Systems;

public sealed class WhiteDamageModifierStatusEffectSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WhiteDamageModifierStatusEffectComponent, StatusEffectRelayedEvent<DamageModifyEvent>>(OnDamageModify);
    }

    private void OnDamageModify(Entity<WhiteDamageModifierStatusEffectComponent> ent, ref StatusEffectRelayedEvent<DamageModifyEvent> args)
    {
        DamageSpecifier newDamage = new();
        foreach (var (type, damage) in args.Args.Damage.DamageDict)
        {
            var dmg = damage * ent.Comp.GlobalDefence;

            if (ent.Comp.Defence is not null && ent.Comp.Defence.TryGetValue(type, out var typeDefence))
                dmg *= typeDefence;

            newDamage.DamageDict[type] = dmg;
        }
        args.Args.Damage = newDamage;
    }
}
