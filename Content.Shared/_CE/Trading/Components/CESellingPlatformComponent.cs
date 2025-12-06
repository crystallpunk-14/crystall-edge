using Content.Shared._CE.Trading.Systems;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Trading.Components;

/// <summary>
/// Allows you to sell items by overloading the platform with energy
/// </summary>
[RegisterComponent, Access(typeof(CESharedTradingPlatformSystem))]
public sealed partial class CESellingPlatformComponent : Component
{
    [DataField]
    public SoundSpecifier SellSound = new SoundPathSpecifier("/Audio/_CE/Effects/cash.ogg")
    {
        Params = AudioParams.Default.WithVariation(0.1f),
    };

    [DataField]
    public EntProtoId SellVisual = "CECashImpact";
}
