using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Power.Components;

/// <summary>
/// Component for energy transfer gloves that can drain or transfer battery charge between entities.
/// 
/// Used by <see cref="Content.Server._CE.Power.CEPowerSystem"/> in TransferGlove partial.
/// On AfterInteract, transfers energy between user and target batteries, applying knockback/pull effects.
/// Drain mode pulls target toward user while draining; transfer mode pushes target away while charging.
/// Use in hand toggles between modes, updating networked state.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CEEnergyTransferGloveComponent : Component
{
    /// <summary>
    /// Amount of charge to transfer per interaction. In drain mode, attempts to drain this much from target.
    /// In transfer mode, attempts to spend this much from user to charge target.
    /// </summary>
    [DataField]
    public float TransferAmount = 5f;

    /// <summary>
    /// Operating mode: true = drain energy from target and pull it toward user, false = transfer energy to target and push it away.
    /// Toggleable via use in hand. State is networked for client display.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool ConsumeMode = true;

    /// <summary>
    /// Throw strength applied when pushing or pulling the target entity.
    /// </summary>
    [DataField]
    public float ThrowPower = 5f;

    /// <summary>
    /// Distance in tiles to push the target away from user in transfer mode.
    /// </summary>
    [DataField]
    public float ThrowDistance = 1f;

    /// <summary>
    /// Distance in tiles to pull the target toward user in drain mode.
    /// Scaled by the percentage of charge successfully drained.
    /// </summary>
    [DataField]
    public float PullDistance = 1f;

    /// <summary>
    /// Sound played when using the glove on a target.
    /// </summary>
    [DataField]
    public SoundSpecifier UseSound = new SoundCollectionSpecifier("sparks");

    /// <summary>
    /// Sound played when switching to drain (consume) mode.
    /// </summary>
    [DataField]
    public SoundSpecifier ConsumeModeSound = new SoundPathSpecifier("/Audio/Items/flashlight_on.ogg");

    /// <summary>
    /// Sound played when switching to transfer mode.
    /// </summary>
    [DataField]
    public SoundSpecifier TransferModeSound = new SoundPathSpecifier("/Audio/Items/flashlight_off.ogg");

    /// <summary>
    /// Visual effect spawned at target's location when transferring energy.
    /// </summary>
    [DataField]
    public EntProtoId VFX = "CEOverchargeSmallVFX";
}
