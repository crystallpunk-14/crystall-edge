using Content.Shared._CE.Actions.Spells;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._CE.Weapons.MeleeEnergyEffect;

/// <summary>
/// Component that adds energy-based melee effects to weapons.
/// This component manages the activation and deactivation of special energy effects on melee weapons,
/// including effect triggering on hits, battery charge tracking, and audio-visual feedback.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true), AutoGenerateComponentPause]
public sealed partial class CEMeleeEnergyEffectComponent : Component
{
    /// <summary>
    /// Whether the energy effect is currently active on this weapon.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Active;

    /// <summary>
    /// Energy cost in battery units required to activate the effect.
    /// </summary>
    [DataField]
    public float EnergyRequired = 10f;

    /// <summary>
    /// If true, a charged effect attack does not deal standard weapon damage.
    /// </summary>
    [DataField]
    public bool RemoveBaseDamage = true;

    /// <summary>
    /// List of spell effects to apply to targets hit while the weapon is active.
    /// </summary>
    [DataField(required: true)]
    public List<CESpellEffect> Effects = new();

    /// <summary>
    /// Current number of available hits before battery depletion.
    /// Batteries aren't predicted, so this value is manually tracked and synchronized.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int Hits;

    /// <summary>
    /// Maximum number of available hits based on total battery capacity.
    /// Calculated as MaxCharge / EnergyRequired.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int Capacity;

    /// <summary>
    /// Duration for which the effect remains active after activation.
    /// </summary>
    [DataField]
    public TimeSpan ActiveDuration = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Time when the active effect should automatically deactivate.
    /// Set to zero when the effect is inactive.
    /// </summary>
    [DataField, AutoNetworkedField, AutoPausedField]
    public TimeSpan DeactivateTime = TimeSpan.Zero;

    /// <summary>
    /// Sound to play when the energy effect is activated.
    /// </summary>
    [DataField]
    public SoundSpecifier ActivateSound = new SoundPathSpecifier("/Audio/_CE/Effects/energy_charge.ogg", new AudioParams{Variation = 0.15f});

    /// <summary>
    /// Sound to play when the energy effect is deactivated.
    /// </summary>
    [DataField]
    public SoundSpecifier DeactivateSound = new SoundPathSpecifier("/Audio/_CE/Effects/energy_pulse2.ogg", new AudioParams{Variation = 0.15f});

    [DataField]
    public SoundSpecifier NoEnergySound = new SoundCollectionSpecifier("sparks");
}

[Serializable, NetSerializable]
public enum CEMeleeEnergyState : byte
{
    Active,
};
