using Content.Server._CE.GameTicking;
using Content.Shared._CE.Trade;
using Content.Shared._CE.Trade.Components;
using Content.Shared._CE.Trade.MainQuest;
using Content.Shared._CE.Trade.Rewards;

namespace Content.Server._CE.Trade.MainQuest;

/// <summary>
/// Stocks quest postaments with their reward for the round's price.
/// </summary>
public sealed partial class CEMainQuestTradeSystem : EntitySystem
{
    [Dependency] private CESharedTradeSystem _trade = default!;
    [Dependency] private CEMurkConsumingRuleSystem _rule = default!;

    [SubscribeLocalEvent]
    private void OnFillOffer(Entity<CETradeQuestTableComponent> ent, ref CETradeTableFillOfferEvent args)
    {
        if (args.Handled)
            return;

        if (!_rule.TryGetPrice(ent.Comp.PriceIndex, out var price))
        {
            Log.Debug($"{ToPrettyString(ent)} has no rolled price number {ent.Comp.PriceIndex} - nothing to offer.");
            return;
        }

        _trade.FillOffer(args.Offer,
            price.Cost,
            [new CETradeSpawnReward { Proto = ent.Comp.Reward }],
            payPrice: price.Pay,
            receivePrice: 0);

        args.Handled = true;
    }

    [SubscribeLocalEvent]
    private void OnRoundStart(CERoundStartEvent args)
    {
        if (!_rule.TryGetPriceCount(out var priceCount))
            return;

        var present = new HashSet<int>();
        var query = EntityQueryEnumerator<CETradeQuestTableComponent>();
        while (query.MoveNext(out _, out var table))
        {
            present.Add(table.PriceIndex);
        }

        for (var i = 1; i <= priceCount; i++)
        {
            if (!present.Contains(i))
                Log.Warning($"No quest postament for price number {i} on the map - that purchase is impossible this round.");
        }
    }
}
