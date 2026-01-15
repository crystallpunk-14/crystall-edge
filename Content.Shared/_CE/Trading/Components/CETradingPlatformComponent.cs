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
    public EntProtoId BuyVisual = "CECashImpact";

    [DataField]
    public SoundSpecifier SellSound = new SoundPathSpecifier("/Audio/_CE/Effects/cash.ogg")
    {
        Params = AudioParams.Default.WithVariation(0.1f)
    };

    [DataField]
    public EntProtoId SellVisual = "CECashImpact";

    [DataField(required: true)]
    public ProtoId<CETradingFactionPrototype> Faction = default!;
}
