using Content.Shared._CE.Trading.Prototypes;
using Content.Shared._CE.Trading.Systems;
using Content.Shared.Tag;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Trading.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(CESharedTradingPlatformSystem))]
public sealed partial class CETradingPlatformComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan NextBuyTime = TimeSpan.Zero;

    [DataField]
    public SoundSpecifier BuySound = new SoundPathSpecifier("/Audio/_CE/Effects/cash.ogg")
    {
        Params = AudioParams.Default.WithVariation(0.1f)
    };

    [DataField]
    public ProtoId<TagPrototype> CoinTag = "CECoin";

    [DataField]
    public EntProtoId? BuyVisual;

    [DataField]
    public SoundSpecifier? SellSound;

    [DataField]
    public EntProtoId? SellVisual;

    [DataField(required: true)]
    public ProtoId<CETradingFactionPrototype> Faction = default!;

    /// <summary>
    /// Where purchased items go. False (default) spawns them next to the platform.
    /// True tries to put them directly into the buyer's hand instead (falling back to a drop
    /// if their hands are full) - used by discreet platforms like the black market.
    /// </summary>
    [DataField]
    public bool GiveToBuyerHand;

    /// <summary>
    /// Whether this platform lets players sell items/fulfill requests. False for one-way
    /// platforms like the black market, which has nothing physical to place items on.
    /// </summary>
    [DataField]
    public bool SupportSelling = true;
}
