using Content.Shared._CE.Farming.Components;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;

namespace Content.Server._CE.Farming;

public sealed partial class CEFarmingSystem
{
    [Dependency] private DamageableSystem _damageable = default!;

    private void InitializeHealth()
    {
        SubscribeLocalEvent<CEPlantHealingComponent, CEPlantUpdateEvent>(OnPlantHealing);
        SubscribeLocalEvent<CEPlantFadingComponent, CEAfterPlantUpdateEvent>(OnPlantFading);
        SubscribeLocalEvent<CEPlantDyingComponent, DamageChangedEvent>(OnPlantDamageChanged);
    }

    private void OnPlantDamageChanged(Entity<CEPlantDyingComponent> ent, ref DamageChangedEvent args)
    {
        if (!args.DamageIncreased)
            return;

        var withering = _damageable.GetPositiveDamage((ent.Owner, args.Damageable), ent.Comp.DamageGroup);
        if (withering.GetTotal() < ent.Comp.DeathThreshold)
            return;

        if (ent.Comp.DeadEntity is { } dead)
            SpawnAtPosition(dead, Transform(ent).Coordinates);

        QueueDel(ent.Owner);
    }

    private void OnPlantHealing(Entity<CEPlantHealingComponent> ent, ref CEPlantUpdateEvent args)
    {
        var plant = args.Plant.Comp;

        if (!TryComp<DamageableComponent>(ent, out var damageable))
            return;

        var currentDamage = _damageable.GetPositiveDamage((ent.Owner, damageable));

        var hasHealableDamage = false;
        foreach (var type in ent.Comp.Heal.DamageDict.Keys)
        {
            if (currentDamage.DamageDict.ContainsKey(type))
            {
                hasHealableDamage = true;
                break;
            }
        }

        if (!hasHealableDamage)
            return;

        if (plant.Energy < ent.Comp.EnergyCost || plant.Resource < ent.Comp.ResourceCost)
            return;

        AffectEnergy(args.Plant, -ent.Comp.EnergyCost);
        AffectResource(args.Plant, -ent.Comp.ResourceCost);

        _damageable.TryChangeDamage((ent.Owner, damageable), -ent.Comp.Heal, ignoreResistances: true, interruptsDoAfters: false);
    }

    private void OnPlantFading(Entity<CEPlantFadingComponent> ent, ref CEAfterPlantUpdateEvent args)
    {
        if (args.Plant.Comp.Resource > 0)
            return;

        _damageable.TryChangeDamage(ent.Owner, ent.Comp.Damage, interruptsDoAfters: false);
    }
}
