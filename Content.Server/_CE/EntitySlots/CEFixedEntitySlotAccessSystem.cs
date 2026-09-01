using Content.Shared._CE.EntitySlots;
using Robust.Shared.Containers;

namespace Content.Server._CE.EntitySlots;

/// <summary>
/// Maintains the replicated access marker for occupants of opt-in fixed-slot hosts.
/// </summary>
public sealed partial class CEFixedEntitySlotAccessSystem : EntitySystem
{
    [Dependency] private CEFixedEntitySlotSystem _slots = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CEFixedEntitySlotAccessComponent, ComponentStartup>(OnHostStartup);
        SubscribeLocalEvent<CEFixedEntitySlotAccessComponent, ComponentShutdown>(OnHostShutdown);
        SubscribeLocalEvent<CEFixedEntitySlotAccessComponent, EntInsertedIntoContainerMessage>(OnInserted);
        SubscribeLocalEvent<CEFixedEntitySlotAccessComponent, EntRemovedFromContainerMessage>(OnRemoved);
    }

    private void OnHostStartup(Entity<CEFixedEntitySlotAccessComponent> ent, ref ComponentStartup args)
    {
        SyncOccupants(ent.Owner, add: true);
    }

    private void OnHostShutdown(Entity<CEFixedEntitySlotAccessComponent> ent, ref ComponentShutdown args)
    {
        if (TerminatingOrDeleted(ent.Owner))
            return;

        SyncOccupants(ent.Owner, add: false);
    }

    private void OnInserted(
        Entity<CEFixedEntitySlotAccessComponent> ent,
        ref EntInsertedIntoContainerMessage args)
    {
        if (_slots.TryGetSlot(args.Entity, out var host, out _) && host == ent.Owner)
            EnsureComp<CEFixedEntitySlotAccessibleOccupantComponent>(args.Entity);
    }

    private void OnRemoved(
        Entity<CEFixedEntitySlotAccessComponent> ent,
        ref EntRemovedFromContainerMessage args)
    {
        RemComp<CEFixedEntitySlotAccessibleOccupantComponent>(args.Entity);
    }

    private void SyncOccupants(EntityUid host, bool add)
    {
        if (!TryComp<CEFixedEntitySlotsComponent>(host, out var slots))
            return;

        foreach (var occupant in _slots.GetOccupants((host, slots)))
        {
            if (occupant is not { } uid)
                continue;

            if (add)
                EnsureComp<CEFixedEntitySlotAccessibleOccupantComponent>(uid);
            else
                RemComp<CEFixedEntitySlotAccessibleOccupantComponent>(uid);
        }
    }
}
