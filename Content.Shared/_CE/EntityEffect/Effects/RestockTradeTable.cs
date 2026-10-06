using Content.Shared._CE.Trade;
using Content.Shared._CE.Trade.Components;
using Robust.Shared.Network;

namespace Content.Shared._CE.EntityEffect.Effects;

/// <summary>
/// Puts a random offer of the table's shop into one of its empty slots.
/// </summary>
public sealed partial class RestockTradeTable : CEEntityEffectBase<RestockTradeTable>
{
}

public sealed partial class CERestockTradeTableEffectSystem : CEEntityEffectSystem<RestockTradeTable>
{
    [Dependency] private INetManager _net = default!;
    [Dependency] private CESharedTradeSystem _trade = default!;

    [Dependency] private EntityQuery<CETradeTableComponent> _tableQuery = default!;

    protected override void Effect(ref CEEntityEffectEvent<RestockTradeTable> args)
    {
        if (_net.IsClient)
            return;

        if (ResolveEffectEntity(args.Args, args.Effect.EffectTarget) is not { } target)
            return;

        if (!_tableQuery.TryComp(target, out var table))
            return;

        _trade.TryRestock((target, table));
    }
}
