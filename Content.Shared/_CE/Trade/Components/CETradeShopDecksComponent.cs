using Content.Shared._CE.Trade.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Trade.Components;

/// <summary>
/// Station-wide rotation of shop offers: every offer is shown once before any is repeated.
/// Offers still waiting to be shown are kept per shop; an empty deck is rebuilt and reshuffled.
/// </summary>
[RegisterComponent, Access(typeof(CESharedTradeSystem))]
public sealed partial class CETradeShopDecksComponent : Component
{
    [DataField]
    public Dictionary<ProtoId<CETradeShopPrototype>, List<ProtoId<CETradeOfferPrototype>>> Decks = new();
}
