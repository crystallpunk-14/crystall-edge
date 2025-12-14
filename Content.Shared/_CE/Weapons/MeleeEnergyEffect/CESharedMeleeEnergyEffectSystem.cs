using Content.Shared._CE.Actions.Spells;
using Content.Shared.Interaction.Events;
using Content.Shared.Timing;
using Content.Shared.Weapons.Melee.Events;

namespace Content.Shared._CE.Weapons.MeleeEnergyEffect;

public abstract class CESharedMeleeEnergyEffectSystem : EntitySystem
{
    [Dependency] private readonly UseDelaySystem _useDelay = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CEMeleeEnergyEffectComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<CEMeleeEnergyEffectComponent, MeleeHitEvent>(OnMeleeAttack);
    }

    protected virtual void OnUseInHand(Entity<CEMeleeEnergyEffectComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        if (ent.Comp.Active)
            return;

        if (TryComp<UseDelayComponent>(ent, out var delay))
        {
            if (_useDelay.IsDelayed((ent.Owner, delay), ent.Comp.UseDelayKey))
                return;

            _useDelay.TryResetDelay((ent.Owner, delay), false, ent.Comp.UseDelayKey);
        }

        args.Handled = true;
        //Predicted audio or popup for instant feedback
    }

    private void OnMeleeAttack(Entity<CEMeleeEnergyEffectComponent> ent, ref MeleeHitEvent args)
    {
        if (!ent.Comp.Active)
            return;

        if (!args.IsHit)
            return;

        foreach (var target in args.HitEntities)
        {
            foreach (var effect in ent.Comp.Effects)
            {
                effect.Effect(EntityManager, new CESpellEffectBaseArgs(args.User, ent, target, Transform(target).Coordinates));
            }
        }

        ent.Comp.Active = false;
        DirtyField(ent, ent.Comp, nameof(CEMeleeEnergyEffectComponent.Active));
    }
}
