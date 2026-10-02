using Content.Shared._CE.Radio.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Radio.Components;

/// <summary>
/// City loudspeaker. When powered, repeats speech heard by any <see cref="CERadioMicrophoneComponent"/>
/// broadcasting on one of its frequencies within the microphone's radius.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CERadioSpeakerComponent : Component
{
    [DataField, AutoNetworkedField]
    public HashSet<ProtoId<CERadioFrequencyPrototype>> Frequencies = new();
}
