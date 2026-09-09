using Content.Shared.Containers.ItemSlots;
using Content.Shared.Interaction;
using Content.Shared.Lock;
using Content.Shared.Storage.Components;
using Robust.Shared.Containers;

namespace Content.Shared._CE.Containers;

/// <summary>Shares the same container access policy between discovery and normal interaction.</summary>
public sealed partial class CEOpenContainerSystem : EntitySystem
{
    [Dependency] private SharedContainerSystem _containers = default!;
    [Dependency] private SharedInteractionSystem _interaction = default!;
    [Dependency] private ItemSlotsSystem _itemSlots = default!;

    /// <summary>
    /// Grants container accessibility only. Range, obstruction, ingestion and pickup checks remain native.
    /// An opted-in fixture becomes private again while carried, worn, or nested inside another container.
    /// </summary>
    public bool CanAccessContents(EntityUid user, BaseContainer container)
    {
        var host = container.Owner;
        if (TerminatingOrDeleted(host) ||
            !TryComp<CEOpenContainerComponent>(host, out var open) ||
            !open.Containers.Contains(container.ID) ||
            !_containers.TryGetContainer(host, container.ID, out var current) ||
            !ReferenceEquals(current, container) ||
            _containers.IsEntityOrParentInContainer(host) ||
            TryComp<LockComponent>(host, out var locking) && locking.Locked ||
            TryComp<EntityStorageComponent>(host, out var storage) && !storage.Open ||
            TryComp<ItemSlotsComponent>(host, out var slots) &&
            _itemSlots.TryGetSlot((host, slots), container.ID, out var slot) && slot.Locked)
            return false;

        return _interaction.CanAccess(user, host);
    }

    [SubscribeLocalEvent]
    private void OnAccessibleOverride(Entity<TransformComponent> ent, ref AccessibleOverrideEvent args)
    {
        if (args.Handled || args.Target != ent.Owner ||
            !_containers.TryGetContainingContainer(ent.Owner, out var container) ||
            !CanAccessContents(args.User, container))
            return;

        args.Accessible = true;
        args.Handled = true;
    }
}
