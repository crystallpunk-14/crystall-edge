using Content.Shared._CE.MeleeWeapon.Components;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;

namespace Content.Shared._CE.MeleeWeapon;

public abstract partial class CESharedWeaponSystem
{
    [Dependency] private SharedStaminaSystem _stamina = default!;

    private void InitializeCosts()
    {
        SubscribeLocalEvent<CEWeaponStaminaCostComponent, CEWeaponUseAttemptEvent>(OnStaminaCostAttempt);
        SubscribeLocalEvent<CEWeaponStaminaCostComponent, CEWeaponUsedEvent>(OnStaminaCostUsed);
    }

    private void OnStaminaCostAttempt(Entity<CEWeaponStaminaCostComponent> ent, ref CEWeaponUseAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (!ent.Comp.Costs.TryGetValue(args.UseType, out var cost) || cost <= 0f)
            return;

        if (!TryComp<StaminaComponent>(args.User, out var stamina))
            return;

        if (stamina.Critical || stamina.StaminaDamage + cost >= stamina.CritThreshold)
            args.Cancel();
    }

    private void OnStaminaCostUsed(Entity<CEWeaponStaminaCostComponent> ent, ref CEWeaponUsedEvent args)
    {
        if (!ent.Comp.Costs.TryGetValue(args.UseType, out var cost) || cost <= 0f)
            return;

        _stamina.TakeStaminaDamage(args.User, cost, visual: false, ignoreResist: true);
    }
}
