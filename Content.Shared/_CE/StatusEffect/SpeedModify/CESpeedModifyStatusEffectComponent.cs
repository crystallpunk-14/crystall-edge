using Robust.Shared.GameStates;

namespace Content.Shared._CE.StatusEffect.SpeedModify;

/// <summary>
/// Modifies an entity's sprint and walk speeds while a status effect is active.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class CESpeedModifyStatusEffectComponent : Component
{
    [DataField]
    public float Sprint = 1f;

    [DataField]
    public float Walk = 1f;
}
