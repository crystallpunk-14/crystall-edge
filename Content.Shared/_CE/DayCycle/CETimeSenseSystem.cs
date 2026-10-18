using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.StatusEffectNew;

namespace Content.Shared._CE.DayCycle;

/// <summary>
/// Decides whether an entity can sense the time of day: worn clocks and time sense status effects grant it.
/// Recalculated on equip/unequip and status effect changes, see <see cref="CETimeSenseRefreshedEvent"/>.
/// </summary>
public sealed partial class CETimeSenseSystem : EntitySystem
{
    public bool CanSenseTime(EntityUid uid)
    {
        var ev = new CECheckTimeSenseEvent();
        RaiseLocalEvent(uid, ref ev);
        return ev.CanSense;
    }

    public void RefreshTimeSense(EntityUid uid)
    {
        RaiseLocalEvent(new CETimeSenseRefreshedEvent(uid, CanSenseTime(uid)));
    }

    [SubscribeLocalEvent]
    private void OnClockCheck(Entity<CEClockComponent> ent, ref InventoryRelayedEvent<CECheckTimeSenseEvent> args)
    {
        args.Args.CanSense = true;
    }

    [SubscribeLocalEvent]
    private void OnClockEquipped(Entity<CEClockComponent> ent, ref GotEquippedEvent args)
    {
        if ((args.SlotFlags & CECheckTimeSenseEvent.Slots) == 0)
            return;

        RefreshTimeSense(args.EquipTarget);
    }

    [SubscribeLocalEvent]
    private void OnClockUnequipped(Entity<CEClockComponent> ent, ref GotUnequippedEvent args)
    {
        if ((args.SlotFlags & CECheckTimeSenseEvent.Slots) == 0)
            return;

        RefreshTimeSense(args.EquipTarget);
    }

    [SubscribeLocalEvent]
    private void OnStatusEffectCheck(Entity<CETimeSenseStatusEffectComponent> ent, ref StatusEffectRelayedEvent<CECheckTimeSenseEvent> args)
    {
        args.Args = args.Args with { CanSense = true };
    }

    [SubscribeLocalEvent]
    private void OnStatusEffectApplied(Entity<CETimeSenseStatusEffectComponent> ent, ref StatusEffectAppliedEvent args)
    {
        RefreshTimeSense(args.Target);
    }

    [SubscribeLocalEvent]
    private void OnStatusEffectRemoved(Entity<CETimeSenseStatusEffectComponent> ent, ref StatusEffectRemovedEvent args)
    {
        RefreshTimeSense(args.Target);
    }
}
