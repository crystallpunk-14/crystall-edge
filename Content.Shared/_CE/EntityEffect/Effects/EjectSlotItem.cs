using Content.Shared.Containers.ItemSlots;

namespace Content.Shared._CE.EntityEffect.Effects;

/// <summary>
/// Ejects the item from the first occupied <see cref="ItemSlotsComponent"/> slot found on the resolved
/// target entity, regardless of slot name - generic so it can be reused on any slotted entity, not just
/// infusion altar pedestals.
/// </summary>
public sealed partial class EjectSlotItem : CEEntityEffectBase<EjectSlotItem>
{
}

public sealed partial class CEEjectSlotItemEffectSystem : CEEntityEffectSystem<EjectSlotItem>
{
    [Dependency] private ItemSlotsSystem _itemSlots = default!;

    protected override void Effect(ref CEEntityEffectEvent<EjectSlotItem> args)
    {
        if (ResolveEffectEntity(args.Args, args.Effect.EffectTarget) is not { } entity)
            return;

        if (!TryComp<ItemSlotsComponent>(entity, out var slots))
            return;

        foreach (var slot in slots.Slots.Values)
        {
            if (!slot.HasItem)
                continue;

            _itemSlots.TryEject(entity, slot, null, out _);
            break;
        }
    }
}
