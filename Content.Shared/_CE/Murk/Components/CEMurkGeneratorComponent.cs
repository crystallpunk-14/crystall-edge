using Content.Shared._CE.Murk.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared._CE.Murk.Components;

/// <summary>
/// Drives <see cref="CEMurkSourceComponent"/> from mains power: the sphere grows towards its limit
/// while powered and collapses back once the supply is cut, at the same steady rate either way.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true), Access(typeof(CESharedMurkSystem))]
public sealed partial class CEMurkGeneratorComponent : Component
{
    [DataField, AutoNetworkedField]
    public float DisabledIntensity = 0f;

    [DataField, AutoNetworkedField]
    public float EnabledIntensity = -5f;

    /// <summary>
    /// How much intensity is gained or lost per second while moving towards the current target.
    /// Since intensity doubles as the radius, the sphere visibly grows and shrinks from its edges.
    /// </summary>
    [DataField]
    public float ChangeRate = 0.5f;
}
