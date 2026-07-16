using Content.Shared._CE.Animation.Item.Components;
using Robust.Shared.GameStates;

namespace Content.Shared._CE.MeleeWeapon.Components;

/// <summary>
/// Replaces attack animations for the item being used if the item is held in both hands (wielded).
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true)]
[Access(typeof(CESharedWeaponSystem))]
public sealed partial class CEWieldedWeaponComponent : Component
{
    /// <summary>
    /// Mapping from input button to attack action prototype.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public Dictionary<CEUseType, List<CEAnimationEntry>> Animations = new();
}
