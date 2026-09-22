using Robust.Shared.Serialization;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Content.Shared._CE.Trading.Prototypes;
using Content.Shared.Actions;

namespace Content.Shared._CE.Trading;

/// <summary>
/// Opens the trading UI carried by the action entity itself (not the performer) - the real work
/// happens in <see cref="ActionPerformedEvent"/>, which the engine always raises directed at the
/// action entity regardless of RaiseOnAction/RaiseOnUser, so this event only needs a catch-all
/// handler somewhere to mark it handled.
/// </summary>
public sealed partial class CEOpenTradingUiEvent : InstantActionEvent
{
}

[Serializable, NetSerializable]
public enum CETradingUiKey
{
    Buy,
    Sell,
}

[Serializable, NetSerializable]
public sealed class CETradingPlatformUiState(NetEntity platform, int sellBalance, ProtoId<CETradingFactionPrototype> faction, bool supportSelling) : BoundUserInterfaceState
{
    public NetEntity Platform = platform;
    public int SellBalance = sellBalance;
    public ProtoId<CETradingFactionPrototype> Faction = faction;
    public bool SupportSelling = supportSelling;
}

[Serializable, NetSerializable]
public readonly struct CETradingProductEntry
{
}

/// <summary>
/// Broadcast whenever a purchase on any trading platform succeeds - lets unrelated systems (e.g.
/// objective conditions) react without the trading system needing to know about them.
/// </summary>
[ByRefEvent]
public readonly record struct CEPlatformPurchaseEvent(EntityUid Buyer, ProtoId<CETradingFactionPrototype> Faction, int Price);
