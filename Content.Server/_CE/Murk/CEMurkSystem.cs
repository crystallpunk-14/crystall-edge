using Content.Server.Power.EntitySystems;
using Content.Shared._CE.Murk.Components;
using Content.Shared._CE.Murk.Systems;
using Content.Shared.Power.Components;

namespace Content.Server._CE.Murk;

public sealed partial class CEMurkSystem : CESharedMurkSystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly BatterySystem _battery = default!;

    private EntityQuery<CEMurkedMapComponent> _mapQuery;

    public override void Initialize()
    {
        base.Initialize();

        _mapQuery = GetEntityQuery<CEMurkedMapComponent>();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<BatteryComponent, CEMurkGeneratorComponent>();
        while (query.MoveNext(out var uid, out var battery, out var murkGen))
        {
            if (!_mapQuery.TryComp(_transform.GetMap(uid), out var murkedMap) || murkedMap.Intensity <= 0)
                continue;

            var dischargeRate = murkedMap.Intensity * murkGen.NetLoadPerMapIntensity * frameTime;
            _battery.SetCharge(uid, battery.CurrentCharge - dischargeRate, battery);
        }
    }
}
