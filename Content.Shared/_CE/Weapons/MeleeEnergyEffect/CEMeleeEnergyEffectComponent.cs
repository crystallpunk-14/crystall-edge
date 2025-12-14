using Content.Shared._CE.Actions.Spells;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._CE.Weapons.MeleeEnergyEffect;

/// <summary>
///
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true), AutoGenerateComponentPause]
public sealed partial class CEMeleeEnergyEffectComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Active;

    [DataField]
    public float EnergyRequired = 10f;

    [DataField]
    public string UseDelayKey = "charging";

    [DataField(required: true)]
    public List<CESpellEffect> Effects = new();

    // Batteries aren't predicted which means we need to track the battery and manually count it ourselves woo!
    [DataField, AutoNetworkedField]
    public int Hits;

    [DataField, AutoNetworkedField]
    public int Capacity;

    [DataField]
    public TimeSpan ActiveDuration = TimeSpan.FromSeconds(5);

    [DataField, AutoNetworkedField, AutoPausedField]
    public TimeSpan DeactivateTime = TimeSpan.Zero;

    [DataField]
    public SoundSpecifier ActivateSound = new SoundPathSpecifier("/Audio/Effects/sparks1.ogg"); //TODO normal sound

    [DataField]
    public SoundSpecifier DeactivateSound = new SoundPathSpecifier("/Audio/Effects/sparks2.ogg"); //TODO normal sound
}

[Serializable, NetSerializable]
public enum CEMeleeEnergyState : byte
{
    Active,
};
