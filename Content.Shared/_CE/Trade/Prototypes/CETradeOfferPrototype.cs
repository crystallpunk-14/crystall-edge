using Content.Shared._CE.ResourceManager;
using Content.Shared._CE.Trade.Rewards;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Trade.Prototypes;

/// <summary>
/// An exchange: <see cref="Cost"/> and <see cref="Pay"/> coins for <see cref="Reward"/> and <see cref="Receive"/> coins.
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
    /// Coins the player pays. Auto-priced from <see cref="Reward"/> when no amount is set.
    /// </summary>
    [DataField]
    public CETradeMoney? Pay;

    /// <summary>
    /// Coins the player receives. Auto-priced from <see cref="Cost"/> when no amount is set.
    /// </summary>
    [DataField]
    public CETradeMoney? Receive;

    /// <summary>
    /// Entity whose sprite represents the offer. Optional.
    /// </summary>
    [DataField]
    public EntProtoId? Preview;

    [DataField]
    public LocId? Name;
}

[DataDefinition]
public sealed partial class CETradeMoney
{
    /// <summary>
    /// Fixed amount before variation. Null means auto-priced from the other side of the exchange.
    /// </summary>
    [DataField]
    public int? Amount;

    [DataField]
    public float Markup = 1f;

    /// <summary>
    /// Flat amount added to the auto-priced value after <see cref="Markup"/>.
    /// </summary>
    [DataField]
    public int Bonus;

    /// <summary>
    /// Price is rolled within +-this fraction when the offer is placed.
    /// </summary>
    [DataField]
    public float Variation = 0.1f;
}
