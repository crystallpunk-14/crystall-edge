using Robust.Shared.GameStates;

namespace Content.Shared._CE.StatusEffect.SpeedModify;

/// <summary>
///
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class CESpeedModifyStatusEffectComponent : Component
{
    [DataField]
    public float Sprint = 1f;

    [DataField]
    public float Walk = 1f;
}
