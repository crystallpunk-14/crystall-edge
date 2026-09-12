using Content.Shared.Movement.Systems;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Analyzers;

namespace Content.Shared._CE.StatusEffect.SpeedModify;

public sealed partial class CESpeedModifyStatusEffectSystem : EntitySystem
{
    [Dependency] private MovementSpeedModifierSystem _speedModifier = default!;

    [SubscribeLocalEvent]
    private void OnApplied(Entity<CESpeedModifyStatusEffectComponent> ent, ref StatusEffectAppliedEvent args)
    {
        _speedModifier.RefreshMovementSpeedModifiers(args.Target);
    }

    [SubscribeLocalEvent]
    private void OnRemoved(Entity<CESpeedModifyStatusEffectComponent> ent, ref StatusEffectRemovedEvent args)
    {
        _speedModifier.RefreshMovementSpeedModifiers(args.Target);
    }

    [SubscribeLocalEvent]
    private void OnUpdateSpeed(Entity<CESpeedModifyStatusEffectComponent> ent, ref StatusEffectRelayedEvent<RefreshMovementSpeedModifiersEvent> args)
    {
        args.Args.ModifySpeed(ent.Comp.Walk, ent.Comp.Sprint);
    }
}
