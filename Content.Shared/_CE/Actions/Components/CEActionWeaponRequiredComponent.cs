namespace Content.Shared._CE.Actions.Components;

/// <summary>
/// Requires the user to be holding a CE weapon (<see cref="Content.Shared._CE.Animation.Item.Components.CEWeaponComponent"/>)
/// in their active hand to use this action.
/// </summary>
[RegisterComponent]
public sealed partial class CEActionWeaponRequiredComponent : Component
{
}
