using Content.Shared._CE.Animation.Item.Components;
using Robust.Shared.GameStates;

namespace Content.Shared._CE.MeleeWeapon.Components;

/// <summary>
/// When present on a weapon entity, requires the user to spend stamina to attack.
/// Costs are defined per <see cref="CEUseType"/> (Primary, Secondary, etc.).
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class CEWeaponStaminaCostComponent : Component
{
    /// <summary>
    /// Stamina cost per use type.
    /// </summary>
    [DataField(required: true)]
    public Dictionary<CEUseType, float> Costs = new();
}
