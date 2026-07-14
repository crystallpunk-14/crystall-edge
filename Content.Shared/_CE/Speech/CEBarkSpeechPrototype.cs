using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Speech;

/// <summary>
/// Defines a bark voice profile: sounds for different intonations,
/// timing, and pitch parameters.
/// </summary>
[Prototype("barkSpeech")]
public sealed partial class CEBarkSpeechPrototype : IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// Default speech sound (declarative sentences).
    /// </summary>
    [DataField(required: true)]
    public SoundSpecifier SaySound = default!;

    /// <summary>
    /// Sound for question intonation (syllables near ?).
    /// Falls back to <see cref="SaySound"/> if null.
    /// </summary>
    [DataField]
    public SoundSpecifier? AskSound;

    /// <summary>
    /// Sound for exclamation intonation (syllables near !).
    /// Falls back to <see cref="SaySound"/> if null.
    /// </summary>
    [DataField]
    public SoundSpecifier? ExclaimSound;

    /// <summary>
    /// Time in seconds between each syllable.
    /// </summary>
    [DataField]
    public float SyllableInterval = 0.08f;

    /// <summary>
    /// How many characters per syllable (1 bark per N characters).
    /// </summary>
    [DataField]
    public int CharsPerSyllable = 2;

    /// <summary>
    /// Pause multiplier for spaces between words (times SyllableInterval).
    /// </summary>
    [DataField]
    public float WordPauseMultiplier = 2.5f;

    /// <summary>
    /// Pause multiplier for commas.
    /// </summary>
    [DataField]
    public float CommaPauseMultiplier = 4.0f;

    /// <summary>
    /// Random pitch variation per syllable (gaussian sigma).
    /// </summary>
    [DataField]
    public float PitchVariation = 0.05f;

    /// <summary>
    /// Pitch rise for question sentences (?).
    /// Applied progressively over the last ~30% of syllables.
    /// </summary>
    [DataField]
    public float QuestionPitchRise = 0.25f;

    /// <summary>
    /// Pitch drop for declarative sentences (.).
    /// Applied progressively over the last ~20% of syllables.
    /// </summary>
    [DataField]
    public float DeclarativePitchDrop = 0.1f;

    /// <summary>
    /// Volume boost for exclamation sentences (!).
    /// </summary>
    [DataField]
    public float ExclaimVolumeBoost = 3f;

    /// <summary>
    /// Pitch boost for exclamation sentences.
    /// </summary>
    [DataField]
    public float ExclaimPitchBoost = 0.15f;

    /// <summary>
    /// Maximum syllables to play per message.
    /// </summary>
    [DataField]
    public int MaxSyllables = 40;

    /// <summary>
    /// Audio parameters for bark sounds.
    /// </summary>
    [DataField]
    public AudioParams AudioParams = AudioParams.Default.WithVolume(-2f).WithRolloffFactor(4.5f);

    /// <summary>
    /// Whether players can select this bark voice in the character editor.
    /// Set to false for NPC-only voices.
    /// </summary>
    [DataField]
    public bool Selectable = true;
}
