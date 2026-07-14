using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Speech;

/// <summary>
/// When placed on an entity, makes it produce Animal Crossing-style "bark" sounds
/// when speaking. References a <see cref="CEBarkSpeechPrototype"/> for voice profile.
/// </summary>
[RegisterComponent]
public sealed partial class CEBarkSpeechComponent : Component
{
    /// <summary>
    /// The bark voice profile to use.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<CEBarkSpeechPrototype> BarkSpeech;

    /// <summary>
    /// Per-entity pitch override (multiplied with character-derived pitch).
    /// </summary>
    [DataField]
    public float BasePitch = 1.0f;
}
