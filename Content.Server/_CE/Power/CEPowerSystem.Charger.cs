using System.Linq;
using Content.Server._CE.Power.Components;
using Content.Server.Audio;
using Content.Server.Power.EntitySystems;
using Content.Shared.Placeable;
using Content.Shared.Power;

namespace Content.Server._CE.Power;

public sealed partial class CEPowerSystem
{
    [Dependency] private readonly AmbientSoundSystem _ambient = default!;
    [Dependency] private readonly BatterySystem _battery = default!;

    private void InitializeCharger()
    {
        SubscribeLocalEvent<CEChargingPlatformComponent, PowerChangedEvent>(OnChargerChange);
    }

    private void OnChargerChange(Entity<CEChargingPlatformComponent> ent, ref PowerChangedEvent args)
    {
        var enabled = args.Powered;
        _ambient.SetAmbience(ent, enabled);
        _pointLight.SetEnabled(ent, enabled);
    }

    private void UpdateChargers(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<CEChargingPlatformComponent, ItemPlacerComponent>();
        while (query.MoveNext(out var uid, out var charger, out var itemPlacer))
        {
            if (_timing.CurTime < charger.NextCharge)
                continue;

            if (!this.IsPowered(uid, EntityManager))
                continue;

            charger.NextCharge = _timing.CurTime + charger.Frequency;

            if (!itemPlacer.PlacedEntities.Any())
                continue;

            foreach (var placed in itemPlacer.PlacedEntities)
            {
                _battery.ChangeCharge(placed, charger.Charge / itemPlacer.PlacedEntities.Count);
            }
        }
    }
}
