using System.Linq;
using Content.Server.Power.EntitySystems;
using Content.Shared._CE.Power.Components;
using Content.Shared.Placeable;

namespace Content.Server._CE.Power;

public sealed partial class CEPowerSystem
{
    private void UpdateChargers(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<CEChargingPlatformComponent, ItemPlacerComponent>();
        while (query.MoveNext(out var uid, out var charger, out var itemPlacer))
        {
            if (Timing.CurTime < charger.NextCharge)
                continue;

            if (!this.IsPowered(uid, EntityManager))
                continue;

            charger.NextCharge = Timing.CurTime + charger.Frequency;

            if (!itemPlacer.PlacedEntities.Any())
                continue;

            foreach (var placed in itemPlacer.PlacedEntities)
            {
                // Try to get battery from PowerCell slot first, fallback to direct BatteryComponent
                if (PowerCell.TryGetBatteryFromSlot((placed, null), out var battery))
                    Battery.ChangeCharge((battery.Value.Owner, battery.Value.Comp), charger.Charge / itemPlacer.PlacedEntities.Count);
                else if (BatteryQuery.TryComp(placed, out var directBattery))
                    Battery.ChangeCharge((placed, directBattery), charger.Charge / itemPlacer.PlacedEntities.Count);
            }
        }
    }
}
