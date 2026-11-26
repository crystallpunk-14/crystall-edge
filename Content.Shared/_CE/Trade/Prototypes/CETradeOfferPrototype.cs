using Content.Shared._CE.ResourceManager;
using Content.Shared._CE.Trade.Rewards;
using Content.Shared._CE.Workbench.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Trade.Prototypes;

/// <summary>
/// An exchange of <see cref="Cost"/> for <see cref="Reward"/>. An offer with only a reward is bought for coins,
/// an offer with only a cost is paid for in coins, an offer with both is a barter without coins.
/// </summary>
[Prototype("tradeOffer")]
public sealed partial class CETradeOfferPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public ProtoId<CETradeShopPrototype> Shop;

    [DataField]
    public float Weight = 1f;

    /// <summary>
    /// Items the player hands over.
    /// </summary>
    [DataField]
    public List<CEResourceRequirement> Cost = new();

    /// <summary>
    /// What the player gets besides coins.
    /// </summary>
    [DataField]
    public List<CETradeReward> Reward = new();

    /// <summary>
    /// Coins added to the cost price of the items on the priced side of the exchange.
    /// </summary>
    [DataField]
    public int Bonus;

    /// <summary>
    /// Entity whose sprite represents the offer. Optional.
    /// </summary>
    [DataField]
    public EntProtoId? Preview;

    [DataField]
    public LocId? Name;

    /// <summary>
    /// Section the offer is listed under in trade shop windows.
    /// </summary>
    [DataField]
    public ProtoId<CEWorkbenchRecipeCategoryPrototype>? Category;
}
