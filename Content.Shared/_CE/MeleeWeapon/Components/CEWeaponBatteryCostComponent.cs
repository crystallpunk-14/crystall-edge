using Content.Shared._CE.Animation.Item.Components;
using Robust.Shared.GameStates;

namespace Content.Shared._CE.MeleeWeapon.Components;

/// <summary>
/// When present on a weapon entity, requires the user's weapon to spend battery charge to attack.
/// Costs are defined per <see cref="CEUseType"/> (Primary, Secondary, etc.).
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class CEWeaponBatteryCostComponent : Component
{
    /// <summary>
    /// Battery charge cost per use type.
    /// </summary>
    [DataField(required: true)]
    public Dictionary<CEUseType, float> Costs = new();
}
