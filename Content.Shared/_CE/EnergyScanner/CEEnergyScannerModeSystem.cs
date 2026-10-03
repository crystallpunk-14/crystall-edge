using Content.Shared.Actions;
using Content.Shared.Inventory;
using Content.Shared.Popups;

namespace Content.Shared._CE.EnergyScanner;

/// <summary>
/// Grants the mode action of <see cref="CEEnergyScannerClothingComponent"/> to the wearer and cycles the overlay mode.
/// </summary>
public sealed partial class CEEnergyScannerModeSystem : EntitySystem
{
    [Dependency] private ActionContainerSystem _actionContainer = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    [SubscribeLocalEvent]
    private void OnMapInit(Entity<CEEnergyScannerClothingComponent> ent, ref MapInitEvent args)
    {
        _actionContainer.EnsureAction(ent, ref ent.Comp.ActionEntity, ent.Comp.Action);
        Dirty(ent);
    }

    [SubscribeLocalEvent]
    private void OnGetActions(Entity<CEEnergyScannerClothingComponent> ent, ref GetItemActionsEvent args)
    {
        if (args.SlotFlags is not { } slot || (slot & SlotFlags.EYES) == 0)
            return;

        args.AddAction(ent.Comp.ActionEntity);
    }

    [SubscribeLocalEvent]
    private void OnCycleMode(Entity<CEEnergyScannerClothingComponent> ent, ref CEEnergyScannerCycleModeEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        var modeCount = Enum.GetValues<CEEnergyScannerMode>().Length;
        ent.Comp.Mode = (CEEnergyScannerMode) (((int) ent.Comp.Mode + 1) % modeCount);
        Dirty(ent);

        _popup.PopupClient(Loc.GetString(GetModeLocId(ent.Comp.Mode)), args.Performer, args.Performer);
    }

    private static string GetModeLocId(CEEnergyScannerMode mode)
    {
        return mode switch
        {
            CEEnergyScannerMode.Below => "ce-energy-scanner-mode-below",
            CEEnergyScannerMode.Current => "ce-energy-scanner-mode-current",
            CEEnergyScannerMode.Above => "ce-energy-scanner-mode-above",
            _ => "ce-energy-scanner-mode-off",
        };
    }
}
