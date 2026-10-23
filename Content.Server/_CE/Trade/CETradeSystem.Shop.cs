using Content.Shared._CE.Trade;
using Content.Shared._CE.Trade.Components;
using Content.Shared._CE.Trade.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server._CE.Trade;

public sealed partial class CETradeSystem
{
    [SubscribeLocalEvent]
    private void OnTradeShopOpened(Entity<CETradeShopComponent> ent, ref BoundUIOpenedEvent args)
    {
        UpdateShopUi(ent, args.Actor);
    }

    [SubscribeLocalEvent]
    private void OnTradeShopTrade(Entity<CETradeShopComponent> ent, ref CETradeShopTradeMessage args)
    {
        if (!Proto.TryIndex(args.Offer, out var offer) || offer.Shop != ent.Comp.Shop)
            return;

        var (pay, receive) = GetShopPrices(ent, offer);
        TryExchange(args.Actor, ent, args.Actor, offer.Cost, offer.Reward, pay, receive, ent.Comp.TradeSound);
        UpdateShopUi(ent, args.Actor);
    }

    private void UpdateShopUi(Entity<CETradeShopComponent> ent, EntityUid user)
    {
        var items = CollectTradeableItems(user);
        var entries = new List<CETradeShopEntry>();

        foreach (var offer in Proto.EnumeratePrototypes<CETradeOfferPrototype>())
        {
            if (offer.Shop != ent.Comp.Shop)
                continue;

            var (pay, receive) = GetShopPrices(ent, offer);
            entries.Add(new CETradeShopEntry(offer.ID, pay, receive, GetMissing(user, items, offer.Cost, pay)));
        }

        Ui.SetUiState(ent.Owner, CETradeShopUiKey.Key, new CETradeShopUiState(entries, Currency.GetPriceTotal(user)));
    }

    private (int Pay, int Receive) GetShopPrices(Entity<CETradeShopComponent> ent, CETradeOfferPrototype offer)
    {
        if (ent.Comp.Prices.TryGetValue(offer.ID, out var prices))
            return prices;

        prices = (GetFixedPrice(offer.Pay, offer, pay: true), GetFixedPrice(offer.Receive, offer, pay: false));
        ent.Comp.Prices[offer.ID] = prices;
        return prices;
    }
}
