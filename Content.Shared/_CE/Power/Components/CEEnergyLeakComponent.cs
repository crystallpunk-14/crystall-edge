using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._CE.Power.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CEEnergyLeakComponent : Component
{
    [DataField, AutoNetworkedField]
    public float CurrentLeak = 0f;

    /// <summary>
    /// How much of the energy received is emitted as radiation?
    /// </summary>
    [DataField]
    public float LeakPercentage = 0.5f;
}

[Serializable, NetSerializable]
public enum CEEnergyLeakVisuals : byte
{
    Enabled,
}
