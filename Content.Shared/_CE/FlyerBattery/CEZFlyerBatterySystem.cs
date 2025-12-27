using Content.Shared._CE.Vehicle;
using Content.Shared._CE.ZLevels.Flight;
using Content.Shared._CE.ZLevels.Flight.Components;
using Content.Shared.Power;
using Content.Shared.Power.Components;
using Content.Shared.Power.EntitySystems;
using Robust.Shared.Timing;

namespace Content.Shared._CE.FlyerBattery;

public sealed class CEZFlyerBatterySystem : EntitySystem
{
    [Dependency] private readonly CESharedZFlightSystem _flight = default!;
    [Dependency] private readonly SharedBatterySystem _battery = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CEZFlyerBatteryComponent, CEStartFlightAttemptEvent>(OnFlightAttempt);
        SubscribeLocalEvent<CEZFlyerBatteryComponent, BatteryStateChangedEvent>(OnBatteryChanged);
    }

    private void OnBatteryChanged(Entity<CEZFlyerBatteryComponent> ent, ref BatteryStateChangedEvent args)
    {
        if (args.NewState != BatteryState.Empty)
            return;

        _flight.DeactivateFlight(ent.Owner);
    }

    private void OnFlightAttempt(Entity<CEZFlyerBatteryComponent> ent, ref CEStartFlightAttemptEvent args)
    {
        if (!TryComp<BatteryComponent>(ent, out var battery))
            return;

        if (battery.LastCharge <= 0f)
            args.Cancel();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<CEZFlyerBatteryComponent, CEZFlyerComponent, BatteryComponent>();
        while (query.MoveNext(out var uid, out var flyBattery, out var fly, out var battery))
        {
            if (_timing.CurTime < flyBattery.NextConsumeTime)
                continue;

            flyBattery.NextConsumeTime = _timing.CurTime + flyBattery.EnergyConsumeFrequency;

            if (!fly.Active)
                continue;

            _battery.UseCharge((uid, battery), flyBattery.EnergyDraw);
        }
    }
}
