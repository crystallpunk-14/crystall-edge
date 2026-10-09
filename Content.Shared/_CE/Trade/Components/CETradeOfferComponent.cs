using System.Numerics;
using Content.Shared._CE.ResourceManager;
using Content.Shared._CE.Trade.Rewards;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Trade.Components;

/// <summary>
/// A single exchange placed on a trade table. Self-contained: whoever stocks the table writes the
/// whole exchange here, see <see cref="CESharedTradeSystem.FillOffer"/>.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(raiseAfterAutoHandleState: true)]
[Access(typeof(CESharedTradeSystem))]
public sealed partial class CETradeOfferComponent : Component
{
    [DataField, AutoNetworkedField]
    public int Slot;

    /// <summary>
    /// Items the buyer hands over.
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<CEResourceRequirement> Cost = new();

    /// <summary>
    /// What the buyer gets besides coins.
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<CETradeReward> Reward = new();

    /// <summary>
    /// Coins the buyer pays.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int PayPrice;

    /// <summary>
    /// Coins the buyer receives.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int ReceivePrice;

    /// <summary>
    /// Entity whose sprite represents the offer. Falls back to the reward, coins or cost when null.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntProtoId? Preview;

    [DataField]
    public Vector2 PreviewScale = new(0.85f, 0.85f);

    [DataField]
    public SoundSpecifier TradeSound = new SoundPathSpecifier("/Audio/_CE/Effects/cash.ogg")
    {
        Params = AudioParams.Default.WithVariation(0.1f),
    };
}
