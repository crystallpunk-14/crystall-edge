using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.StatusEffectNew;
using Content.Shared.StatusEffectNew.Components;
using Robust.Shared.Timing;

namespace Content.Shared._CE.StatusEffect;

public sealed partial class CEDamageModifierStatusEffectSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CEDamageModifierStatusEffectComponent, StatusEffectRelayedEvent<DamageModifyEvent>>(OnDamageModify);
    }

    private void OnDamageModify(Entity<CEDamageModifierStatusEffectComponent> ent, ref StatusEffectRelayedEvent<DamageModifyEvent> args)
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
