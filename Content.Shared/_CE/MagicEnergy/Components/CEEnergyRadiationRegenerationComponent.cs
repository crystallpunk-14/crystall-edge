using Robust.Shared.GameStates;

namespace Content.Shared._CE.MagicEnergy.Components;

/// <summary>
/// Restores energy inside the BatteryComponent attached to the same entity
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class CEEnergyRadiationRegenerationComponent : Component
{
    /// <summary>
    /// How much energy is recovered per unit of radiation received?
    /// </summary>
    [DataField]
    public float Energy = 1f;

    [DataField]
    public TimeSpan NextUpdate = TimeSpan.Zero;

    [DataField]
    public TimeSpan UpdateFrequency = TimeSpan.FromSeconds(1);
}
