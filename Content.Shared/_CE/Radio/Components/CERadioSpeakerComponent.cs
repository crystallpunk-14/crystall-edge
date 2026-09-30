using Robust.Shared.GameStates;

namespace Content.Shared._CE.Radio.Components;

/// <summary>
/// City loudspeaker. When powered, repeats speech heard by any <see cref="CERadioMicrophoneComponent"/>
/// broadcasting on its frequency within the microphone's radius.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CERadioSpeakerComponent : Component
{
    [DataField, AutoNetworkedField]
    public int Frequency;
}
