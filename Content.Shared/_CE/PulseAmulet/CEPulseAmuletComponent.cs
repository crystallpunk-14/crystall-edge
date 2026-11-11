using Robust.Shared.GameStates;

namespace Content.Shared._CE.PulseAmulet;

/// <summary>
/// Belt amulet that reports its wearer's position and condition to every pulse projector while it has charge.
/// Drains its battery while worn.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(CEPulseAmuletSystem))]
public sealed partial class CEPulseAmuletComponent : Component
{
    /// <summary>
    /// Battery charge drained per second while the amulet is worn.
    /// </summary>
    [DataField]
    public float DrainRate = 0.05f;

    /// <summary>
    /// Who wears the amulet right now. Null when it is not equipped.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? Wearer;
}
