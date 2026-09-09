using Content.Shared._CE.AnimalHusbandry.Reproduction;
using Content.Shared._CE.Examine;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Trigger;
using Content.Shared.Trigger.Components;
using Content.Shared.Trigger.Systems;
using Robust.Shared.Containers;

namespace Content.Server._CE.AnimalHusbandry.Reproduction;

/// <summary>Runs the standard incubation timer only inside a live nest's native ItemSlot.</summary>
public sealed partial class CEAnimalIncubationSystem : EntitySystem
{
    [Dependency] private SharedContainerSystem _containers = default!;
    [Dependency] private ItemSlotsSystem _slots = default!;
    [Dependency] private TriggerSystem _trigger = default!;
    [Dependency] private MetaDataSystem _metadata = default!;

    [SubscribeLocalEvent]
    private void OnStartup(Entity<CEAnimalIncubationComponent> ent, ref ComponentStartup args) => Synchronize(ent);

    [SubscribeLocalEvent]
    private void OnInserted(Entity<CEAnimalIncubationComponent> ent, ref EntGotInsertedIntoContainerMessage args) => Synchronize(ent);

    [SubscribeLocalEvent]
    private void OnRemoved(Entity<CEAnimalIncubationComponent> ent, ref EntGotRemovedFromContainerMessage args) => Pause(ent);

    [SubscribeLocalEvent]
    private void OnShutdown(Entity<CEAnimalIncubationComponent> ent, ref ComponentShutdown args)
    {
        if (!TerminatingOrDeleted(ent))
            Pause(ent);
    }

    [SubscribeLocalEvent]
    private void OnAttemptTrigger(Entity<CEAnimalIncubationComponent> ent, ref AttemptTriggerEvent args)
    {
        if (!TryComp<TimerTriggerComponent>(ent, out var timer) || HasNest(ent))
            return;
        if (args.Key != null && args.Key != timer.KeyOut && !timer.KeysIn.Contains(args.Key))
            return;
        Pause(ent);
        args.Cancelled = true;
    }

    public void Synchronize(EntityUid egg)
    {
        if (!TryComp<CEAnimalIncubationComponent>(egg, out var incubation) || !incubation.Fertilized ||
            !TryComp<TimerTriggerComponent>(egg, out var timer))
            return;
        if (!HasNest(egg))
        {
            Pause(egg);
            return;
        }
        if (_trigger.ActivateTimerTrigger((egg, timer)))
        {
            // Native unpausing shifts the deadline by the whole pause. A timer started during
            // that pause must exclude the portion which elapsed before insertion.
            _trigger.TryDelay((egg, timer), -_metadata.GetPauseTime(egg));
        }
    }

    public void Pause(EntityUid egg)
    {
        if (!TryComp<TimerTriggerComponent>(egg, out var timer) || !HasComp<ActiveTimerTriggerComponent>(egg))
            return;
        if (_trigger.GetRemainingTime((egg, timer)) is { } remaining)
        {
            remaining += _metadata.GetPauseTime(egg);
            _trigger.SetDelay((egg, timer), remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero);
        }
        _trigger.StopTimerTrigger((egg, timer));
    }

    private bool HasNest(EntityUid egg)
    {
        return _containers.TryGetContainingContainer(egg, out var container) &&
               !TerminatingOrDeleted(container.Owner) && TryComp<CEAnimalNestComponent>(container.Owner, out var nest) &&
               nest.LifeStage < ComponentLifeStage.Stopping &&
               TryComp<ItemSlotsComponent>(container.Owner, out var slots) && slots.LifeStage < ComponentLifeStage.Stopping &&
               _slots.TryGetSlot((container.Owner, slots), container.ID, out var slot) &&
               slot.ContainerSlot == container && slot.Item == egg &&
               _containers.TryGetContainer(container.Owner, container.ID, out var live) && live == container;
    }

    [SubscribeLocalEvent]
    private void OnExamine(Entity<CEAnimalNestComponent> ent, ref CEExamineAugmentEvent args)
    {
        if (!TryComp<ItemSlotsComponent>(ent, out var slots))
            return;
        var ordinary = 0;
        var fertilized = 0;
        foreach (var slot in slots.Slots.Values)
        {
            if (slot.Item is not { } egg || !TryComp<CEAnimalIncubationComponent>(egg, out var incubation))
                continue;
            if (incubation.Fertilized)
                fertilized++;
            else
                ordinary++;
        }
        args.AddMarkup(Loc.GetString(ent.Comp.ExamineMessage,
            ("ordinary", ordinary), ("fertilized", fertilized), ("capacity", slots.Slots.Count)));
    }
}
