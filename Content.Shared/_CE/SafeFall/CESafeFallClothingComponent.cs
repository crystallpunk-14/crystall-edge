using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.SafeFall;

/// <summary>
/// Rechargeable belt amulet: spends battery charge to slow down a dangerous fall or catch the wearer above a chasm.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(CESafeFallClothingSystem))]
public sealed partial class CESafeFallClothingComponent : Component
{
    [DataField]
    public EntProtoId StatusEffect = "CEStatusEffectSafeFallAmulet";

    [DataField]
    public TimeSpan Duration = TimeSpan.FromSeconds(10);

    [DataField]
    public TimeSpan StunDuration = TimeSpan.FromSeconds(0.5);

    /// <summary>
    /// Battery charge spent per activation. The amulet does nothing if less charge is left.
    /// </summary>
    [DataField]
    public float ChargeCost = 25f;

    /// <summary>
    /// Downward Z velocity at which the amulet activates mid-air.
    /// </summary>
    [DataField]
    public float VelocityThreshold = 4f;

    /// <summary>
    /// Fraction of the falling velocity kept after activation.
    /// </summary>
    [DataField]
    public float VelocityMultiplier = 0f;

    [DataField]
    public SoundSpecifier Sound = new SoundPathSpecifier("/Audio/_CE/Effects/energy_pulse.ogg");
}
