using System.Numerics;
using Content.Shared._CE.Trade.Prototypes;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Trade.Components;

/// <summary>
/// A single exchange placed on a trade table.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(raiseAfterAutoHandleState: true)]
[Access(typeof(CESharedTradeSystem))]
public sealed partial class CETradeOfferComponent : Component
{
    [DataField, AutoNetworkedField]
    public ProtoId<CETradeOfferPrototype>? Offer;

    [DataField, AutoNetworkedField]
    public int Slot;

    /// <summary>
    /// Coins the buyer pays, rolled when the offer was placed.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int PayPrice;

    /// <summary>
    /// Coins the buyer receives, rolled when the offer was placed.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int ReceivePrice;

    [DataField]
    public Vector2 PreviewScale = new(0.85f, 0.85f);

    [DataField]
    public SoundSpecifier TradeSound = new SoundPathSpecifier("/Audio/_CE/Effects/cash.ogg")
    {
        Params = AudioParams.Default.WithVariation(0.1f),
    };
}
