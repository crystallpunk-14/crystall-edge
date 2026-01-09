using Robust.Shared.GameStates;

namespace Content.Shared._CE.FlightStatusEffect;

/// <summary>
/// the entity is caught in a gravitational trap, and its movement between z-levels is restricted. Use only on StatusEffectComponent entities
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class CEGravityCatchedStatusEffectComponent : Component
{
}
