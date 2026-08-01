using Content.Shared.Containers.ItemSlots;
using Content.Shared.Throwing;
using Robust.Shared.Random;

namespace Content.Shared._CE.EntityEffect.Effects;

/// <summary>
/// Ejects the item from the first occupied <see cref="ItemSlotsComponent"/> slot found on the resolved
/// target entity, regardless of slot name - generic so it can be reused on any slotted entity, not just
/// infusion altar pedestals. The ejected item is knocked away in a random direction so it visibly leaves
/// the slot rather than just popping out in place.
/// </summary>
public sealed partial class EjectSlotItem : CEEntityEffectBase<EjectSlotItem>
{
    [DataField]
    public float ThrowPower = 3f;

    [DataField]
    public float ThrowDistance = 1.5f;
}

public sealed partial class CEEjectSlotItemEffectSystem : CEEntityEffectSystem<EjectSlotItem>
{
    [Dependency] private ItemSlotsSystem _itemSlots = default!;
    [Dependency] private ThrowingSystem _throwing = default!;
    [Dependency] private IRobustRandom _random = default!;

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

            if (!_itemSlots.TryEject(entity, slot, null, out var item))
                return;

            var direction = _random.NextAngle().ToVec();
            _throwing.TryThrow(item.Value,
                direction * (args.Effect.ThrowDistance * args.Args.Power),
                args.Effect.ThrowPower * args.Args.Power,
                doSpin: true);
            break;
        }
    }
}