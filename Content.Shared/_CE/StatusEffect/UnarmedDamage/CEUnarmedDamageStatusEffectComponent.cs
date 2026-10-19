using Content.Shared.Damage;
using Robust.Shared.GameStates;

namespace Content.Shared._CE.StatusEffect.UnarmedDamage;

/// <summary>
/// Boosts the damage of the entity's unarmed attacks (fists, kicks) while a status effect is
/// active: damage is multiplied by <see cref="Multiplier"/>, then <see cref="Bonus"/> is added.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class CEUnarmedDamageStatusEffectComponent : Component
{
    [DataField]
    public float Multiplier = 1f;

    [DataField]
    public DamageSpecifier Bonus = new();
}
