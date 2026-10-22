using Content.Shared._CE.Trade.Prototypes;
using Content.Shared.Actions;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._CE.Trade;

public sealed partial class CEOpenTradeShopEvent : InstantActionEvent;

[Serializable, NetSerializable]
public enum CETradeShopUiKey
{
    Key,
}

/// <param name="Missing">What the user lacks for this exchange, or null when it can be made right now.</param>
[Serializable, NetSerializable]
public readonly record struct CETradeShopEntry(
    ProtoId<CETradeOfferPrototype> Offer,
    int PayPrice,
    int ReceivePrice,
    string? Missing);

[Serializable, NetSerializable]
public sealed class CETradeShopUiState(List<CETradeShopEntry> entries, int balance) : BoundUserInterfaceState
{
    public readonly List<CETradeShopEntry> Entries = entries;
    public readonly int Balance = balance;
}

[Serializable, NetSerializable]
public sealed class CETradeShopTradeMessage(ProtoId<CETradeOfferPrototype> offer) : BoundUserInterfaceMessage
{
    public readonly ProtoId<CETradeOfferPrototype> Offer = offer;
}
