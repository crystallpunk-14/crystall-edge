using Content.Shared._CE.EntitySlots;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;

namespace Content.Server._CE.EntitySlots;

/// <summary>
/// Executes the generic fixed-slot creation action and exposes typed domain hooks around the transaction.
/// </summary>
public sealed partial class CEFixedEntitySlotActionSystem : EntitySystem
{
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private CEFixedEntitySlotSystem _slots = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<TransformComponent, CECreateEntityInFixedSlotActionEvent>(OnCreate);
    }

    private void OnCreate(Entity<TransformComponent> performer, ref CECreateEntityInFixedSlotActionEvent args)
    {
        if (args.Handled ||
            !TryComp<EntityTargetActionComponent>(args.Action, out var targetAction) ||
            !_actions.ValidateEntityTarget(performer.Owner, args.Target, (args.Action.Owner, targetAction)) ||
            !_slots.HasFreeSlot(args.Target))
            return;

        var creating = new CEFixedSlotEntityCreatingEvent(args.Target, args.Prototype);
        RaiseLocalEvent(performer.Owner, ref creating);
        if (creating.Cancelled)
            return;

        EntityUid product;
        try
        {
            product = SpawnAtPosition(creating.Prototype, Transform(args.Target).Coordinates);
        }
        catch (Exception exception)
        {
            Log.Warning($"Failed to spawn {creating.Prototype} for fixed-slot action by {ToPrettyString(performer.Owner)}: {exception.Message}");
            return;
        }

        if (!_slots.TryInsert(product, args.Target, out _))
        {
            QueueDel(product);
            return;
        }

        var created = new CEFixedSlotEntityCreatedEvent(args.Target, product, creating.Prototype);
        RaiseLocalEvent(performer.Owner, ref created);
        if (created.Cancelled)
        {
            _slots.TryRemove(product);
            QueueDel(product);
            return;
        }

        args.Handled = true;
    }
}
