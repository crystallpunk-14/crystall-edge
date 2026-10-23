using Content.Shared._CE.Trade.Prototypes;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Trade.Components;

/// <summary>
/// Lets whoever uses this action trade with every offer of <see cref="Shop"/> through a window, at fixed prices.
/// </summary>
[RegisterComponent, Access(typeof(CESharedTradeSystem))]
public sealed partial class CETradeShopComponent : Component
{
    [DataField(required: true)]
    public ProtoId<CETradeShopPrototype> Shop;

    [DataField]
    public SoundSpecifier TradeSound = new SoundPathSpecifier("/Audio/_CE/Effects/cash.ogg")
    {
        Params = AudioParams.Default.WithVariation(0.1f),
    };

    /// <summary>
    /// Fixed pay and receive prices per offer, computed on first use.
    /// </summary>
    [ViewVariables]
    public Dictionary<ProtoId<CETradeOfferPrototype>, (int Pay, int Receive)> Prices = new();
}
