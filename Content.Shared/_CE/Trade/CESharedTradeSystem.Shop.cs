using Content.Shared._CE.Trade.Components;
using Content.Shared.Interaction;

namespace Content.Shared._CE.Trade;

public abstract partial class CESharedTradeSystem
{
    [SubscribeLocalEvent]
    private void OnOpenTradeShop(CEOpenTradeShopEvent args)
    {
        if (args.Handled || !HasComp<CETradeShopComponent>(args.Action))
            return;

        args.Handled = true;
        Ui.TryToggleUi(args.Action.Owner, CETradeShopUiKey.Key, args.Performer);
    }

    /// <summary>
    /// The shop lives on an action inside the user's mind, so it is never physically accessible.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnTradeShopAccessible(Entity<CETradeShopComponent> ent, ref AccessibleOverrideEvent args)
    {
        if (args.Target != ent.Owner)
            return;

        args.Handled = true;
        args.Accessible = true;
    }
}
