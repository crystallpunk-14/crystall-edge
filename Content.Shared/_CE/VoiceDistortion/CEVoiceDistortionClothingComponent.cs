using Content.Shared._CE.Speech;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.VoiceDistortion;

/// <summary>
/// While worn, hides the wearer's speaker name behind a fixed one and replaces their bark voice.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(CEVoiceDistortionSystem))]
public sealed partial class CEVoiceDistortionClothingComponent : Component
{
    [DataField]
    public LocId VoiceName = "ce-voice-distortion-name";

    [DataField]
    public ProtoId<CEBarkSpeechPrototype> BarkSpeech = "DarkSoul";

    [DataField]
    public float BarkPitch = 1.0f;
}
