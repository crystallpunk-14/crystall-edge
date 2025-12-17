using Content.Server.Power.EntitySystems;
using Content.Server.Radiation.Components;
using Content.Shared._CE.MagicEnergy.Components;
using Content.Shared._CE.MagicEnergy.Systems;
using Content.Shared.Electrocution;
using Content.Shared.Examine;
using Content.Shared.Inventory;
using Content.Shared.Power.Components;
using Robust.Shared.Timing;

namespace Content.Server._CE.MagicEnergy;

public sealed partial class CEMagicEnergySystem : CESharedMagicEnergySystem
{
    [Dependency] private readonly BatterySystem _battery = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CEEnergyRadiationArmorComponent, InventoryRelayedEvent<CEEnergyRadiationDefenceCalculateEvent>>((e, c, ev) => OnDefenceCalculate(e, c, ev.Args));
    }

    private void OnDefenceCalculate(EntityUid uid, CEEnergyRadiationArmorComponent armor, CEEnergyRadiationDefenceCalculateEvent args)
    {
        args.AddDefence(armor.Armor);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<RadiationReceiverComponent, CEEnergyRadiationRegenerationComponent, BatteryComponent>();
        while (query.MoveNext(out var uid, out var radReceiver, out var energyRegen, out var battery))
        {
            if (_timing.CurTime < energyRegen.NextUpdate)
                continue;
            energyRegen.NextUpdate = _timing.CurTime + energyRegen.UpdateFrequency;

            var change = radReceiver.CurrentRadiation * energyRegen.Energy;
            if (change == 0)
                continue;

            var ev = new CEEnergyRadiationDefenceCalculateEvent();
            RaiseLocalEvent(uid, ev);

            var multiplier = ev.GetMultiplier();
            if (multiplier == 0)
                continue;

            _battery.ChangeCharge((uid, battery), change * multiplier);
        }
    }
}
