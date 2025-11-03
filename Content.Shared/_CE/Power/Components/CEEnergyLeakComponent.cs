using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._CE.Power.Components;

/// <summary>
/// Works together with PowerConsumerComponent, emitting radiation around itself depending on the level of energy consumed
/// </summary>
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
public enum CEToggleableCableVisuals : byte
{
    Enabled,
}
