using Content.Shared.Inventory;
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

/// <summary>
/// Called each time the entity receives energy from radiation,
/// and transmitted to all equipped items to calculate the defense modifier against the energy received.
/// </summary>
public sealed class CEEnergyRadiationDefenceCalculateEvent : EntityEventArgs, IInventoryRelayEvent
{
    public SlotFlags TargetSlots => SlotFlags.WITHOUT_POCKET;
    private float _multiplier = 1f;

    public void AddDefence(float defence)
    {
        if (defence <= 0)
            return;

        _multiplier = Math.Clamp(_multiplier - defence, 0, 1);
    }

    public float GetMultiplier()
    {
        return _multiplier;
    }
}
