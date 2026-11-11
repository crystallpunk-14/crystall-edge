using Content.Client._CE.HoverBar;
using Content.Client.Overlays;
using Content.Shared._CE.ManaVision;
using Content.Shared.Inventory.Events;
using Content.Shared.Power.Components;
using Content.Shared.Power.EntitySystems;

namespace Content.Client._CE.ManaVision;

/// <summary>
/// While the local player has mana vision, adds a mana bar above the hovered creature or item that stores mana
/// </summary>
public sealed partial class CEManaVisionHudSystem : EquipmentHudSystem<CEManaVisionComponent>
{
    [Dependency] private CEHoverBarSystem _hoverBar = default!;
    [Dependency] private SharedBatterySystem _battery = default!;

    private const string ManaBar = "Mana";
    private const int ManaOrder = 2;

    private static readonly Color ManaColor = Color.FromHex("#3d7bd9");

    protected override void UpdateInternal(RefreshEquipmentHudEvent<CEManaVisionComponent> args)
    {
        base.UpdateInternal(args);
        _hoverBar.SetSourceActive(this, true);
    }

    protected override void DeactivateInternal()
    {
        base.DeactivateInternal();
        _hoverBar.SetSourceActive(this, false);
    }

    [SubscribeLocalEvent]
    private void OnCollectManaBar(Entity<BatteryComponent> ent, ref CECollectHoverBarsEvent args)
    {
        if (!IsActive || ent.Comp.MaxCharge <= 0f)
            return;

        var fill = Math.Clamp(_battery.GetChargeLevel((ent, ent.Comp)), 0f, 1f);
        args.Bars.Add(new CEHoverBar(ManaBar, ManaOrder, fill, ManaColor));
    }
}
