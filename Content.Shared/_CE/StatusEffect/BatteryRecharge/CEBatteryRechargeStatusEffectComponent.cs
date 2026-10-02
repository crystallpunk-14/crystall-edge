using Robust.Shared.GameStates;

namespace Content.Shared._CE.StatusEffect.BatteryRecharge;

/// <summary>
/// Continuously restores or drains the charge of the battery (mana) of whoever the status effect is applied to.
/// Never pushes the battery past its limits, so it can neither trigger overcharge nor deficit damage.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class CEBatteryRechargeStatusEffectComponent : Component
{
    /// <summary>
    /// Charge change per second. Positive restores, negative drains.
    /// </summary>
    [DataField]
    public float Rate = 2f;
}
