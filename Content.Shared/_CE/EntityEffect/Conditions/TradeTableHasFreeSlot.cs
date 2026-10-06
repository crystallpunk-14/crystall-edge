using Content.Shared._CE.Trade;
using Content.Shared._CE.Trade.Components;

namespace Content.Shared._CE.EntityEffect.Conditions;

/// <summary>
/// Passes when the entity is a trade table with at least one empty slot.
/// </summary>
public sealed partial class TradeTableHasFreeSlot : CEEntityConditionBase<TradeTableHasFreeSlot>
{
}

public sealed partial class CETradeTableHasFreeSlotConditionSystem : CEEntityConditionSystem<TradeTableHasFreeSlot>
{
    [Dependency] private CESharedTradeSystem _trade = default!;

    [Dependency] private EntityQuery<CETradeTableComponent> _tableQuery = default!;

    protected override void Condition(ref CEEntityConditionEvent<TradeTableHasFreeSlot> args)
    {
        args.Result = _tableQuery.TryComp(args.Entity, out var table) && _trade.HasFreeSlot((args.Entity, table));
    }
}
