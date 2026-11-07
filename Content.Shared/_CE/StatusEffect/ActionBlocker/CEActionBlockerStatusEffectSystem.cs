using Content.Shared.ActionBlocker;
using Content.Shared.Actions.Components;
using Content.Shared.Actions.Events;
using Content.Shared.CombatMode;
using Content.Shared.Hands;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory.Events;
using Content.Shared.Item;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Pulling.Events;
using Content.Shared.Standing;
using Content.Shared.StatusEffectNew;
using Content.Shared.StatusEffectNew.Components;
using Content.Shared.Throwing;

namespace Content.Shared._CE.StatusEffect.ActionBlocker;

public sealed partial class CEActionBlockerStatusEffectSystem : EntitySystem
{
    [Dependency] private ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private SharedCombatModeSystem _combat = default!;

    [SubscribeLocalEvent]
    private void OnApplied(Entity<CEActionBlockerStatusEffectComponent> ent, ref StatusEffectAppliedEvent args)
    {
        if (ent.Comp.BlockMove)
            _actionBlocker.UpdateCanMove(args.Target);

        if (ent.Comp.BlockAttack && TryComp<CombatModeComponent>(args.Target, out var combat))
            _combat.SetInCombatMode(args.Target, false, combat);
    }

    [SubscribeLocalEvent]
    private void OnRemoved(Entity<CEActionBlockerStatusEffectComponent> ent, ref StatusEffectRemovedEvent args)
    {
        if (ent.Comp.BlockMove)
            _actionBlocker.UpdateCanMove(args.Target);
    }

    [SubscribeLocalEvent]
    private void OnActionAttempt(Entity<ActionComponent> ent, ref ActionAttemptEvent args)
    {
        if (args.Cancelled || !ent.Comp.CheckCanInteract)
            return;

        if (!TryComp<StatusEffectContainerComponent>(args.User, out var container)
            || container.ActiveStatusEffects is not { } effects)
            return;

        foreach (var effect in effects.ContainedEntities)
        {
            if (!TryComp<CEActionBlockerStatusEffectComponent>(effect, out var blocker) || !blocker.BlockActions)
                continue;

            args.Cancelled = true;
            return;
        }
    }

    [SubscribeLocalEvent]
    private void OnInteractAttempt(Entity<CEActionBlockerStatusEffectComponent> ent, ref StatusEffectRelayedEvent<InteractionAttemptEvent> args)
    {
        if (!ent.Comp.BlockInteract)
            return;

        var ev = args.Args;
        ev.Cancelled = true;
        args.Args = ev;
    }

    [SubscribeLocalEvent]
    private void OnMoveAttempt(Entity<CEActionBlockerStatusEffectComponent> ent, ref StatusEffectRelayedEvent<UpdateCanMoveEvent> args)
    {
        if (ent.Comp.BlockMove)
            args.Args.Cancel();
    }

    [SubscribeLocalEvent]
    private void OnAttackAttempt(Entity<CEActionBlockerStatusEffectComponent> ent, ref StatusEffectRelayedEvent<AttackAttemptEvent> args)
    {
        if (ent.Comp.BlockAttack)
            args.Args.Cancel();
    }

    [SubscribeLocalEvent]
    private void OnUseAttempt(Entity<CEActionBlockerStatusEffectComponent> ent, ref StatusEffectRelayedEvent<UseAttemptEvent> args)
    {
        if (ent.Comp.BlockUse)
            args.Args.Cancel();
    }

    [SubscribeLocalEvent]
    private void OnThrowAttempt(Entity<CEActionBlockerStatusEffectComponent> ent, ref StatusEffectRelayedEvent<ThrowAttemptEvent> args)
    {
        if (ent.Comp.BlockThrow)
            args.Args.Cancel();
    }

    [SubscribeLocalEvent]
    private void OnDropAttempt(Entity<CEActionBlockerStatusEffectComponent> ent, ref StatusEffectRelayedEvent<DropAttemptEvent> args)
    {
        if (ent.Comp.BlockDrop)
            args.Args.Cancel();
    }

    [SubscribeLocalEvent]
    private void OnPickupAttempt(Entity<CEActionBlockerStatusEffectComponent> ent, ref StatusEffectRelayedEvent<PickupAttemptEvent> args)
    {
        if (ent.Comp.BlockPickup)
            args.Args.Cancel();
    }

    [SubscribeLocalEvent]
    private void OnPullAttempt(Entity<CEActionBlockerStatusEffectComponent> ent, ref StatusEffectRelayedEvent<PullAttemptEvent> args)
    {
        if (ent.Comp.BlockPull && args.Args.PullerUid == args.AppliedTo)
            args.Args.Cancelled = true;
    }

    [SubscribeLocalEvent]
    private void OnStandAttempt(Entity<CEActionBlockerStatusEffectComponent> ent, ref StatusEffectRelayedEvent<StandAttemptEvent> args)
    {
        if (ent.Comp.BlockStand)
            args.Args.Cancel();
    }

    [SubscribeLocalEvent]
    private void OnEquipAttempt(Entity<CEActionBlockerStatusEffectComponent> ent, ref StatusEffectRelayedEvent<IsEquippingAttemptEvent> args)
    {
        if (ent.Comp.BlockEquip)
            args.Args.Cancel();
    }

    [SubscribeLocalEvent]
    private void OnUnequipAttempt(Entity<CEActionBlockerStatusEffectComponent> ent, ref StatusEffectRelayedEvent<IsUnequippingAttemptEvent> args)
    {
        if (ent.Comp.BlockUnequip)
            args.Args.Cancel();
    }
}
