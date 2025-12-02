using Robust.Shared.GameStates;

namespace Content.Shared._CE.Vampire.Components;

/// <summary>
/// increases the amount of blood essence extracted if the victim is strapped to the altar
/// </summary>
[RegisterComponent]
[NetworkedComponent]
[AutoGenerateComponentState]
[Access(typeof(CESharedVampireSystem))]
public sealed partial class CEVampireAltarComponent : Component
{
    [DataField, AutoNetworkedField]
    public float Multiplier = 2f;
}
