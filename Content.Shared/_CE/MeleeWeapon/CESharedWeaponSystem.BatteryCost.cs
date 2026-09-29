using Content.Shared._CE.MeleeWeapon.Components;
using Content.Shared.Power.EntitySystems;

namespace Content.Shared._CE.MeleeWeapon;

public abstract partial class CESharedWeaponSystem
{
    [Dependency] private SharedBatterySystem _battery = default!;

    [SubscribeLocalEvent]
    private void OnBatteryCostAttempt(Entity<CEWeaponBatteryCostComponent> ent, ref CEWeaponUseAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (!ent.Comp.Costs.TryGetValue(args.UseType, out var cost) || cost <= 0f)
            return;

        if (_battery.GetCharge(ent.Owner) <= 0f)
            args.Cancel();
    }

    [SubscribeLocalEvent]
    private void OnBatteryCostUsed(Entity<CEWeaponBatteryCostComponent> ent, ref CEWeaponUsedEvent args)
    {
        if (!ent.Comp.Costs.TryGetValue(args.UseType, out var cost) || cost <= 0f)
            return;

        _battery.TryUseCharge(ent.Owner, cost);
    }
}
