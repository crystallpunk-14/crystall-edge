using Content.Shared._CE.Murk.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared._CE.Murk.Components;

/// <summary>
/// Sets the base murk value on the map, which can be modified by various sources.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true), Access(typeof(CESharedMurkSystem))]
public sealed partial class CEMurkedMapComponent : Component
{
    [DataField, AutoNetworkedField]
    public float Intensity = 0f;
}
