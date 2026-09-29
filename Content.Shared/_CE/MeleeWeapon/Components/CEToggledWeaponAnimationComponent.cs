using Content.Shared._CE.Animation.Item.Components;
using Robust.Shared.GameStates;

namespace Content.Shared._CE.MeleeWeapon.Components;

/// <summary>
/// Replaces attack animations for the item being used while its <see cref="Content.Shared.Item.ItemToggle.Components.ItemToggleComponent"/> is activated.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true)]
[Access(typeof(CESharedWeaponSystem))]
public sealed partial class CEToggledWeaponAnimationComponent : Component
{
    /// <summary>
    /// Mapping from input button to attack animations, used while the item's toggle is activated.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public Dictionary<CEUseType, List<CEAnimationEntry>> Animations = new();
}
