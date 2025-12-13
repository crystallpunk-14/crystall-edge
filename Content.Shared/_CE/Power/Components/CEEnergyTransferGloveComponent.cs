using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Power.Components;

/// <summary>
/// TODO
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CEEnergyTransferGloveComponent : Component
{
    [DataField]
    public float TransferAmount = 5f;

    /// <summary>
    /// true = drain from target, false = transfer to target
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool ConsumeMode = true;

    [DataField]
    public float ThrowPower = 5f;

    [DataField]
    public float ThrowDistance = 1f;

    [DataField]
    public float PullDistance = 1f;

    [DataField]
    public SoundSpecifier UseSound = new SoundCollectionSpecifier("sparks");

    [DataField]
    public SoundSpecifier ConsumeModeSound = new SoundPathSpecifier("/Audio/Items/flashlight_on.ogg");

    [DataField]
    public SoundSpecifier TransferModeSound = new SoundPathSpecifier("/Audio/Items/flashlight_off.ogg");

    [DataField]
    public EntProtoId VFX = "CEOverchargeSmallVFX";
}
