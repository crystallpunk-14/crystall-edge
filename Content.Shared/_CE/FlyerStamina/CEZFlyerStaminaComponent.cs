using Robust.Shared.GameStates;

namespace Content.Shared._CE.FlyerStamina;

/// <summary>
/// Limit flight by stamina
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentPause, Access(typeof(CEZFlyerStaminaSystem))]
public sealed partial class CEZFlyerStaminaComponent : Component
{
    [DataField]
    public float StaminaDraw = 15f;

    [DataField]
    public TimeSpan StaminaConsumeFrequency = TimeSpan.FromSeconds(1f);

    [DataField, AutoPausedField]
    public TimeSpan NextConsumeTime = TimeSpan.Zero;
}
