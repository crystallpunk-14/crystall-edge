using Content.Shared.Movement.Systems;
using Content.Shared.StatusEffectNew;

namespace Content.Shared._CE.StatusEffect.SpeedModify;

public sealed class CESpeedModifyStatusEffectSystem : EntitySystem
{
    [Dependency] private readonly MovementSpeedModifierSystem _speedModifier = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CESpeedModifyStatusEffectComponent, StatusEffectAppliedEvent>(OnApplied);
        SubscribeLocalEvent<CESpeedModifyStatusEffectComponent, StatusEffectRemovedEvent>(OnRemoved);
        SubscribeLocalEvent<CESpeedModifyStatusEffectComponent, StatusEffectRelayedEvent<RefreshMovementSpeedModifiersEvent>>(OnUpdateSpeed);
    }

    private void OnApplied(Entity<CESpeedModifyStatusEffectComponent> ent, ref StatusEffectAppliedEvent args)
    {
        _speedModifier.RefreshMovementSpeedModifiers(args.Target);
    }

    private void OnRemoved(Entity<CESpeedModifyStatusEffectComponent> ent, ref StatusEffectRemovedEvent args)
    {
        _speedModifier.RefreshMovementSpeedModifiers(args.Target);
    }

    private void OnUpdateSpeed(Entity<CESpeedModifyStatusEffectComponent> ent, ref StatusEffectRelayedEvent<RefreshMovementSpeedModifiersEvent> args)
    {
        args.Args.ModifySpeed(ent.Comp.Walk, ent.Comp.Sprint);
    }
}
