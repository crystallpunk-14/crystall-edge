using Content.Server.Power.EntitySystems;
using Content.Server.Radiation.Components;
using Content.Shared._CE.MagicEnergy.Components;
using Content.Shared._CE.MagicEnergy.Systems;
using Content.Shared.Damage;
using Content.Shared.Power.Components;
using Robust.Shared.Timing;

namespace Content.Server._CE.MagicEnergy;

public sealed partial class CEMagicEnergySystem : CESharedMagicEnergySystem {

    [Dependency] private readonly BatterySystem _battery = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<RadiationReceiverComponent, CEEnergyRadiationRegenerationComponent, BatteryComponent>();
        while (query.MoveNext(out var uid, out var radReceiver, out var energyRegen, out var battery))
        {
            if (_timing.CurTime < energyRegen.NextUpdate)
                continue;
            energyRegen.NextUpdate = _timing.CurTime + energyRegen.UpdateFrequency;

            _battery.ChangeCharge(uid, radReceiver.CurrentRadiation * energyRegen.Energy, battery);
        }
    }
}
