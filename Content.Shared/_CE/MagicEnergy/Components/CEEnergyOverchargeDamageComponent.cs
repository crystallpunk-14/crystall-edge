using Content.Shared.Damage;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.MagicEnergy.Components;

/// <summary>
/// Damages the entity if it receives an excess of energy
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class CEEnergyOverchargeDamageComponent : Component
{
    /// <summary>
    /// Damage received per unit of excess energy
    /// </summary>
    [DataField]
    public DamageSpecifier Damage = new()
    {
        DamageDict = new()
        {
            { "Shock", 1f },
        }
    };

    [DataField]
    public LocId Popup = "ce-energy-overcharge-popup";

    [DataField]
    public EntProtoId VFX = "CEOverchargeVFX";

    [DataField]
    public SoundSpecifier OverchargeSound = new SoundPathSpecifier("/Audio/Magic/rumble.ogg");
}

/// <summary>
/// Damages the entity when it attempts to use more energy than is available (energy deficit)
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class CEEnergyDeficitDamageComponent : Component
{
    /// <summary>
    /// Damage received per unit of energy deficit
    /// </summary>
    [DataField]
    public DamageSpecifier Damage = new()
    {
        DamageDict = new()
        {
            { "Shock", 1f },
        },
    };

    [DataField]
    public LocId Popup = "ce-energy-deficit-popup";

    [DataField]
    public EntProtoId VFX = "CEOverchargeVFX";

    [DataField]
    public SoundSpecifier OverchargeSound = new SoundPathSpecifier("/Audio/Magic/rumble.ogg");
}
