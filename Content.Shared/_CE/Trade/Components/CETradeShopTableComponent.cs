using Content.Shared._CE.Trade.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Trade.Components;

/// <summary>
/// Stocks the trade table with weighted random offers of <see cref="Shop"/>.
/// </summary>
[RegisterComponent, Access(typeof(CESharedTradeSystem))]
public sealed partial class CETradeShopTableComponent : Component
{
    [DataField(required: true)]
    public ProtoId<CETradeShopPrototype> Shop;
}
