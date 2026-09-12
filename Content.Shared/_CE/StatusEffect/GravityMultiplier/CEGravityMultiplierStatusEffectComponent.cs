using Robust.Shared.GameStates;

namespace Content.Shared._CE.StatusEffect.GravityMultiplier;

/// <summary>
/// Multiplies the strength of z-level gravity acting on the entity (e.g. to make it fall slowly, or fall harder).
/// Use only on StatusEffectComponent entities.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CEGravityMultiplierStatusEffectComponent : Component
{
    /// <summary>
    /// Multiplier applied to <see cref="Content.Shared._CE.ZLevels.Core.EntitySystems.CECheckGravityEvent"/>'s
    /// <c>Gravity</c> while this status is active. Values below 1 make the entity fall slower, above 1 make it fall faster.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Multiplier = 0.15f;
}
