using Content.Shared.Inventory.Events;
using Content.Shared.Power;
using Content.Shared.Power.EntitySystems;

namespace Content.Shared._CE.PulseAmulet;

public sealed partial class CEPulseAmuletSystem : EntitySystem
{
    [Dependency] private SharedBatterySystem _battery = default!;

    [SubscribeLocalEvent]
    private void OnEquipped(Entity<CEPulseAmuletComponent> ent, ref GotEquippedEvent args)
    {
        ent.Comp.Wearer = args.EquipTarget;
        Dirty(ent);
        _battery.RefreshChargeRate(ent.Owner);
    }

    [SubscribeLocalEvent]
    private void OnUnequipped(Entity<CEPulseAmuletComponent> ent, ref GotUnequippedEvent args)
    {
        ent.Comp.Wearer = null;
        Dirty(ent);
        _battery.RefreshChargeRate(ent.Owner);
    }

    [SubscribeLocalEvent]
    private void OnRefreshChargeRate(Entity<CEPulseAmuletComponent> ent, ref RefreshChargeRateEvent args)
    {
        if (ent.Comp.Wearer != null)
            args.NewChargeRate -= ent.Comp.DrainRate;
    }
}
