using Content.Shared._CE.EntitySlots;
using Content.Shared.Hands.EntitySystems;
using Robust.Shared.Containers;
using Robust.Shared.Map;

namespace Content.Server._CE.EntitySlots;

/// <summary>
/// Adds authored presentation transforms and deterministic selection to standard
/// <see cref="ContainerSlot"/> ownership.
/// </summary>
public sealed partial class CEFixedEntitySlotSystem : EntitySystem
{
    private const string ContainerPrefix = "ce_fixed_slot_";

    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedContainerSystem _containers = default!;
    [Dependency] private SharedHandsSystem _hands = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CEFixedEntitySlotsComponent, MapInitEvent>(OnHostMapInit);
        SubscribeLocalEvent<CEFixedEntitySlotsComponent, EntityTerminatingEvent>(OnHostTerminating);
        SubscribeLocalEvent<CEFixedEntitySlotsComponent, ComponentShutdown>(OnHostShutdown);
        SubscribeLocalEvent<CEFixedEntitySlotsComponent, EntInsertedIntoContainerMessage>(OnContainerInserted);
        SubscribeLocalEvent<CEFixedEntitySlotsComponent, EntRemovedFromContainerMessage>(OnContainerRemoved);
    }

    public bool HasFreeSlot(EntityUid host)
    {
        return TryComp<CEFixedEntitySlotsComponent>(host, out var slots) && FindFreeSlot((host, slots)) >= 0;
    }

    public bool TryGetOccupant(
        Entity<CEFixedEntitySlotsComponent> host,
        int slot,
        out EntityUid occupant)
    {
        occupant = default;
        if (slot < 0 || slot >= host.Comp.Slots.Count ||
            EnsureSlotContainer(host, slot).ContainedEntity is not { } contained)
            return false;

        occupant = contained;
        return true;
    }

    public bool TryInsert(EntityUid occupant, EntityUid host, out int slot)
    {
        slot = -1;
        return TryComp<CEFixedEntitySlotsComponent>(host, out var slots) &&
            TryInsert(occupant, (host, slots), out slot);
    }

    public bool TryInsert(
        EntityUid occupant,
        Entity<CEFixedEntitySlotsComponent> host,
        out int slot)
    {
        slot = -1;
        var freeSlot = FindFreeSlot(host);
        if (freeSlot < 0 || !TryInsertAtSlot(occupant, host, freeSlot))
            return false;

        slot = freeSlot;
        return true;
    }

    /// <summary>
    /// Moves a held entity into the first free fixed slot through the canonical hands transaction.
    /// Internal systems that do not act on behalf of a player should use <see cref="TryInsert(EntityUid, EntityUid, out int)"/>.
    /// </summary>
    public bool TryInsertFromHand(
        EntityUid user,
        EntityUid occupant,
        EntityUid host,
        out int slot)
    {
        slot = -1;
        return TryComp<CEFixedEntitySlotsComponent>(host, out var slots) &&
            TryInsertFromHand(user, occupant, (host, slots), out slot);
    }

    /// <summary>
    /// Moves a held entity into the first free fixed slot through the canonical hands transaction.
    /// </summary>
    public bool TryInsertFromHand(
        EntityUid user,
        EntityUid occupant,
        Entity<CEFixedEntitySlotsComponent> host,
        out int slot)
    {
        slot = -1;
        var freeSlot = FindFreeSlot(host);
        if (freeSlot < 0 ||
            !TryGetInsertContainer(occupant, host, freeSlot, out var container) ||
            !_hands.TryDropIntoContainer(user, occupant, container))
            return false;

        slot = freeSlot;
        return true;
    }

    /// <summary>
    /// Replaces an occupant without bypassing the authored fixed-slot transform or lifecycle events.
    /// </summary>
    public bool TryReplace(EntityUid occupant, EntityUid replacement)
    {
        if (!TryGetSlot(occupant, out var hostUid, out var slot, out var container) ||
            !TryComp<CEFixedEntitySlotsComponent>(hostUid, out var slots) ||
            !TryComp(hostUid, out TransformComponent? hostTransform) ||
            !_containers.CanInsert(replacement, container, assumeEmpty: true) ||
            !_containers.Remove(occupant, container, destination: hostTransform.Coordinates))
            return false;

        // Removal callbacks may revoke or replace the authored slot owner.
        // Leave the original occupant outside instead of inserting through stale state.
        if (slots.LifeStage >= ComponentLifeStage.Stopping ||
            !TryComp<CEFixedEntitySlotsComponent>(hostUid, out var currentSlots) ||
            !ReferenceEquals(slots, currentSlots))
            return false;

        var host = new Entity<CEFixedEntitySlotsComponent>(hostUid, slots);
        if (TryInsertAtSlot(replacement, host, slot))
            return true;

        if (!TryInsertAtSlot(occupant, host, slot))
            Log.Error($"Could not restore {ToPrettyString(occupant)} to fixed slot {slot} in {ToPrettyString(hostUid)} after a failed replacement.");

        return false;
    }

    private bool TryInsertAtSlot(
        EntityUid occupant,
        Entity<CEFixedEntitySlotsComponent> host,
        int slot)
    {
        return TryGetInsertContainer(occupant, host, slot, out var container) &&
            _containers.Insert(occupant, container);
    }

    private bool TryGetInsertContainer(
        EntityUid occupant,
        Entity<CEFixedEntitySlotsComponent> host,
        int slot,
        out ContainerSlot container)
    {
        container = default!;
        if (occupant == host.Owner ||
            TerminatingOrDeleted(occupant) ||
            TerminatingOrDeleted(host.Owner) ||
            TryGetSlot(occupant, out _, out _) ||
            slot < 0 ||
            slot >= host.Comp.Slots.Count ||
            !HasComp<TransformComponent>(occupant))
            return false;

        container = EnsureSlotContainer(host, slot);
        if (container.ContainedEntity != null)
            return false;

        return true;
    }

    public bool TryRemove(EntityUid occupant)
    {
        if (!TryGetSlot(occupant, out _, out _, out var container))
            return false;

        return _containers.Remove(occupant, container);
    }

    public bool TryGetSlot(EntityUid occupant, out EntityUid host, out int slot)
    {
        return TryGetSlot(occupant, out host, out slot, out _);
    }

    private bool TryGetSlot(
        EntityUid occupant,
        out EntityUid host,
        out int slot,
        out ContainerSlot container)
    {
        host = default;
        slot = -1;
        container = default!;
        if (!_containers.TryGetContainingContainer(occupant, out var containing) ||
            containing is not ContainerSlot fixedSlot ||
            !TryComp<CEFixedEntitySlotsComponent>(containing.Owner, out var slots) ||
            !TryGetSlotIndex(slots, containing, out slot))
            return false;

        host = containing.Owner;
        container = fixedSlot;
        return true;
    }

    private void OnHostMapInit(Entity<CEFixedEntitySlotsComponent> ent, ref MapInitEvent args)
    {
        for (var slot = 0; slot < ent.Comp.Slots.Count; slot++)
        {
            var container = EnsureSlotContainer(ent, slot);
            if (container.ContainedEntity is not { } occupant)
                continue;

            ApplyPresentation(occupant, ent.Comp.Slots[slot]);
        }

        SyncAvailability(ent);
    }

    private void OnHostTerminating(
        Entity<CEFixedEntitySlotsComponent> ent,
        ref EntityTerminatingEvent args)
    {
        if (CanEjectOccupants(ent.Owner))
            EjectAll(ent);
    }

    private void OnHostShutdown(Entity<CEFixedEntitySlotsComponent> ent, ref ComponentShutdown args)
    {
        if (!TerminatingOrDeleted(ent.Owner))
            EjectAll(ent);

        RemCompDeferred<CEFixedEntitySlotsAvailableComponent>(ent.Owner);
    }

    private void OnContainerInserted(
        Entity<CEFixedEntitySlotsComponent> ent,
        ref EntInsertedIntoContainerMessage args)
    {
        if (args.Container.Owner != ent.Owner ||
            !TryGetSlotIndex(ent.Comp, args.Container, out var slot))
            return;

        ApplyPresentation(args.Entity, ent.Comp.Slots[slot]);
        SyncAvailability(ent);
        RaiseInserted(args.Entity, ent.Owner, slot);
    }

    private void OnContainerRemoved(
        Entity<CEFixedEntitySlotsComponent> ent,
        ref EntRemovedFromContainerMessage args)
    {
        if (args.Container.Owner != ent.Owner || !TryGetSlotIndex(ent.Comp, args.Container, out var slot))
            return;

        ClearPresentation(args.Entity);
        SyncAvailability(ent);
        RaiseRemoved(args.Entity, ent.Owner, slot);
    }

    private int FindFreeSlot(Entity<CEFixedEntitySlotsComponent> host)
    {
        for (var slot = 0; slot < host.Comp.Slots.Count; slot++)
        {
            if (EnsureSlotContainer(host, slot).ContainedEntity == null)
                return slot;
        }

        return -1;
    }

    private ContainerSlot EnsureSlotContainer(Entity<CEFixedEntitySlotsComponent> host, int slot)
    {
        while (host.Comp.Containers.Count <= slot)
            host.Comp.Containers.Add(null);

        // A different system may replace the canonical container without replacing our component.
        if (host.Comp.Containers[slot] is { } cached &&
            _containers.TryGetContainer(host.Owner, cached.ID, out var current) &&
            ReferenceEquals(cached, current))
            return cached;

        var container = _containers.EnsureContainer<ContainerSlot>(host.Owner, GetContainerId(slot));
        container.ShowContents = true;
        container.OccludesLight = false;
        host.Comp.Containers[slot] = container;
        return container;
    }

    private void ApplyPresentation(EntityUid occupant, CEFixedEntitySlotDefinition definition)
    {
        var appearance = EnsureComp<AppearanceComponent>(occupant);
        _appearance.SetData(occupant, CEFixedSlotVisuals.Offset, definition.Offset, appearance);
        _appearance.SetData(occupant, CEFixedSlotVisuals.Rotation, definition.Rotation, appearance);
        _appearance.SetData(occupant, CEFixedSlotVisuals.Active, true, appearance);
    }

    private void ClearPresentation(EntityUid occupant)
    {
        if (TryComp<AppearanceComponent>(occupant, out var appearance))
            _appearance.SetData(occupant, CEFixedSlotVisuals.Active, false, appearance);
    }

    private void EjectAll(Entity<CEFixedEntitySlotsComponent> host)
    {
        for (var slot = 0; slot < host.Comp.Slots.Count; slot++)
        {
            var container = EnsureSlotContainer(host, slot);
            if (container.ContainedEntity is { } occupant)
            {
                // Component/host teardown must not leave occupants in orphaned
                // fixed-slot containers. Normal player extraction remains subject
                // to the canonical container and Hands removal checks.
                _containers.Remove(occupant, container, force: true);
            }
        }
    }

    private bool CanEjectOccupants(EntityUid host)
    {
        if (!TryComp(host, out TransformComponent? transform) ||
            transform.MapUid is not { } map ||
            TerminatingOrDeleted(map))
            return false;

        return transform.GridUid is not { } grid || !TerminatingOrDeleted(grid);
    }

    private void SyncAvailability(Entity<CEFixedEntitySlotsComponent> host)
    {
        var available = !TerminatingOrDeleted(host.Owner) && FindFreeSlot(host) >= 0;
        if (available)
            EnsureComp<CEFixedEntitySlotsAvailableComponent>(host.Owner);
        else
            RemCompDeferred<CEFixedEntitySlotsAvailableComponent>(host.Owner);
    }

    private static string GetContainerId(int slot) => $"{ContainerPrefix}{slot}";

    private static bool TryGetSlotIndex(
        CEFixedEntitySlotsComponent slots,
        BaseContainer container,
        out int slot)
    {
        var containerId = container.ID;
        if (!containerId.StartsWith(ContainerPrefix, StringComparison.Ordinal) ||
            !int.TryParse(containerId.AsSpan(ContainerPrefix.Length), out slot) ||
            slot < 0 ||
            slot >= slots.Slots.Count ||
            !string.Equals(containerId, GetContainerId(slot), StringComparison.Ordinal))
        {
            slot = -1;
            return false;
        }

        return true;
    }

    private void RaiseInserted(EntityUid occupant, EntityUid host, int slot)
    {
        var ev = new CEFixedEntitySlotInsertedEvent(host, slot);
        RaiseLocalEvent(occupant, ref ev);
    }

    private void RaiseRemoved(EntityUid occupant, EntityUid host, int slot)
    {
        if (!Exists(occupant))
            return;

        var ev = new CEFixedEntitySlotRemovedEvent(host, slot);
        RaiseLocalEvent(occupant, ref ev);
    }
}
