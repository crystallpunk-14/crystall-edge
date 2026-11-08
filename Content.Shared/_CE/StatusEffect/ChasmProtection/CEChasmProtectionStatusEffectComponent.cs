using Robust.Shared.GameStates;

namespace Content.Shared._CE.StatusEffect.ChasmProtection;

/// <summary>
/// Keeps the entity from falling into a chasm: it hovers at the ground level instead. Use only on StatusEffectComponent entities.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class CEChasmProtectionStatusEffectComponent : Component
{
}
