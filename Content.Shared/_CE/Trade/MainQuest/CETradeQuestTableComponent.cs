using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Trade.MainQuest;

/// <summary>
/// Trade table that always offers <see cref="Reward"/> for the round's price number <see cref="PriceIndex"/>.
/// Shadow traders restock it like any table but never clear it at night.
/// </summary>
[RegisterComponent]
public sealed partial class CETradeQuestTableComponent : Component
{
    [DataField(required: true)]
    public EntProtoId Reward;

    /// <summary>
    /// 1-based index into the round's prices.
    /// </summary>
    [DataField(required: true)]
    public int PriceIndex;
}
