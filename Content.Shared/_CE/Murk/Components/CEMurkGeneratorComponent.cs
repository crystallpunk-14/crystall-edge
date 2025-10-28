using Content.Shared._CE.Murk.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared._CE.Murk.Components;

/// <summary>
/// Controls <see cref="CEMurkSourceComponent"/>, changing its intensity depending on the presence or absence of energy
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true), Access(typeof(CESharedMurkSystem))]
public sealed partial class CEMurkGeneratorComponent : Component
{
    [DataField, AutoNetworkedField]
    public float DisabledIntensity = 0f;

    [DataField, AutoNetworkedField]
    public float EnabledIntensity = -5f;

    /// <summary>
    /// constantly drains battery power, depending on the murk level in the world around.
    /// </summary>
    [DataField]
    public float NetLoadPerMapIntensity = 100f;
}
