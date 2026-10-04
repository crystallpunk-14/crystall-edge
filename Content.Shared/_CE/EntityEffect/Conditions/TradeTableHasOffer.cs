using Content.Shared._CE.Trade;
using Content.Shared._CE.Trade.Components;

namespace Content.Shared._CE.EntityEffect.Conditions;

/// <summary>
/// Passes when the entity is a trade table with at least one offer on it.
/// </summary>
public sealed partial class TradeTableHasOffer : CEEntityConditionBase<TradeTableHasOffer>
{
}

public sealed partial class CETradeTableHasOfferConditionSystem : CEEntityConditionSystem<TradeTableHasOffer>
{
    [Dependency] private CESharedTradeSystem _trade = default!;

    [Dependency] private EntityQuery<CETradeTableComponent> _tableQuery = default!;

    protected override void Condition(ref CEEntityConditionEvent<TradeTableHasOffer> args)
    {
        args.Result = _tableQuery.HasComp(args.Entity) && _trade.HasOffer(args.Entity);
    }
}
