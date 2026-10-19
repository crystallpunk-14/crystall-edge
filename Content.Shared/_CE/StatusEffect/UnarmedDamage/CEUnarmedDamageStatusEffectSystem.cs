using Content.Shared._CE.EntityEffect.Effects;
using Content.Shared.StatusEffectNew;

namespace Content.Shared._CE.StatusEffect.UnarmedDamage;

public sealed partial class CEUnarmedDamageStatusEffectSystem : EntitySystem
{
    [SubscribeLocalEvent]
    private void OnOutgoingDamage(Entity<CEUnarmedDamageStatusEffectComponent> ent, ref StatusEffectRelayedEvent<CEGetOutgoingDamageEvent> args)
    {
        // Unarmed - the attack was made with the attacker's own body.
        if (args.Args.Used != args.AppliedTo)
            return;

        args.Args.Damage = args.Args.Damage * ent.Comp.Multiplier + ent.Comp.Bonus;
    }
}
