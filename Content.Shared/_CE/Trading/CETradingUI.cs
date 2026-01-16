using Robust.Shared.Serialization;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Content.Shared._CE.Trading.Prototypes;

namespace Content.Shared._CE.Trading;

[Serializable, NetSerializable]
public enum CETradingUiKey
{
    Buy,
    Sell,
}

[Serializable, NetSerializable]
public sealed class CETradingPlatformUiState(NetEntity platform, int buyBalance, int sellBalance, ProtoId<CETradingFactionPrototype> faction) : BoundUserInterfaceState
{
    public NetEntity Platform = platform;
    public int BuyBalance = buyBalance;
    public int SellBalance = sellBalance;
    public ProtoId<CETradingFactionPrototype> Faction = faction;
}

[Serializable, NetSerializable]
public readonly struct CETradingProductEntry
{
}
