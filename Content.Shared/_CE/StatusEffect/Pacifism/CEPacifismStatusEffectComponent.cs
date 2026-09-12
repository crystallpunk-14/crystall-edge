using Robust.Shared.GameStates;

namespace Content.Shared._CE.StatusEffect.Pacifism;

/// <summary>
/// While active, blocks the target's melee attacks, gunshots and throws via relayed attempt events.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class CEPacifismStatusEffectComponent : Component;
