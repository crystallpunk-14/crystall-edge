using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Trade.Prototypes;

[Prototype("tradeShop")]
public sealed partial class CETradeShopPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;
}
