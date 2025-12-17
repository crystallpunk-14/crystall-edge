using Content.Shared._CE.MagicEnergy.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared._CE.MagicEnergy.Components;

/// <summary>
/// Provides mitigation for magic-energy gained from radiation by subtracting from the radiation defence multiplier
/// collected from equipped items during <see cref="CEEnergyRadiationDefenceCalculateEvent"/>.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(CESharedMagicEnergySystem))]
public sealed partial class CEEnergyRadiationArmorComponent : Component
{
    /// <summary>
    /// Fraction of radiation-derived energy blocked by this item; aggregated with other equipped items and clamped
    /// between 0 and 1 when calculating the final energy gain multiplier.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Armor = 0f;
}
