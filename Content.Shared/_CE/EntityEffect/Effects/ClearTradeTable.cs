using Content.Shared._CE.Trade;
using Content.Shared._CE.Trade.Components;
using Robust.Shared.Network;

namespace Content.Shared._CE.EntityEffect.Effects;

/// <summary>
/// Removes a random offer from the trade table.
/// </summary>
public sealed partial class ClearTradeTable : CEEntityEffectBase<ClearTradeTable>
{
}

public sealed partial class CEClearTradeTableEffectSystem : CEEntityEffectSystem<ClearTradeTable>
{
    [Dependency] private INetManager _net = default!;
    [Dependency] private CESharedTradeSystem _trade = default!;

    [Dependency] private EntityQuery<CETradeTableComponent> _tableQuery = default!;

    protected override void Effect(ref CEEntityEffectEvent<ClearTradeTable> args)
    {
        if (_net.IsClient)
            return;

        if (ResolveEffectEntity(args.Args, args.Effect.EffectTarget) is not { } target)
            return;

        if (!_tableQuery.TryComp(target, out var table))
            return;

        _trade.TryClearOne((target, table));
    }
}
