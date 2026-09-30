using Content.Shared._CE.Radio.Prototypes;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Radio.Components;

/// <summary>
/// Radio booth. While enabled and powered, listens to nearby speech and relays it to every powered
/// <see cref="CERadioSpeakerComponent"/> on one of its frequencies within <see cref="Radius"/>.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CERadioMicrophoneComponent : Component
{
    [DataField, AutoNetworkedField]
    public List<ProtoId<CERadioFrequencyPrototype>> Frequencies = new();

    /// <summary>
    /// Max effective distance (z-level aware) to the loudspeakers that will repeat the speech.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Radius = 30f;

    /// <summary>
    /// How close a speaker has to be to the booth to be heard.
    /// </summary>
    [DataField]
    public float ListenRange = 2f;

    [DataField, AutoNetworkedField]
    public bool Enabled;

    /// <summary>
    /// Played when the booth is switched on.
    /// </summary>
    [DataField, AutoNetworkedField]
    public SoundSpecifier? SoundOn = new SoundPathSpecifier("/Audio/Machines/machine_switch.ogg");

    /// <summary>
    /// Played when the booth is switched off. Same clip as <see cref="SoundOn"/>, pitched down.
    /// </summary>
    [DataField, AutoNetworkedField]
    public SoundSpecifier? SoundOff = new SoundPathSpecifier("/Audio/Machines/machine_switch.ogg", AudioParams.Default.WithPitchScale(0.8f));
}
