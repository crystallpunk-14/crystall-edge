using System.Text.RegularExpressions;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared._CE.Speech;

/// <summary>
/// Shared bark speech base: syllable generation, pitch calculation, sentence parsing.
/// Server and client systems inherit this to provide their own audio playback.
/// </summary>
public abstract partial class CESharedBarkSpeechSystem : EntitySystem
{
    [Dependency] private IRobustRandom _random = default!;

    private static readonly Regex MarkupRegex = new(@"\[.*?\]", RegexOptions.Compiled);

    // CrystallEdge: pitch range that maps to speech speed scaling
    public const float MinPitchScale = 0.75f;
    public const float MaxPitchScale = 1.5f;
    // CrystallEdge end

    /// <summary>
    /// Strips rich-text markup tags from a chat message.
    /// </summary>
    protected static string StripMarkup(string message)
    {
        return MarkupRegex.Replace(message, string.Empty);
    }

    /// <summary>
    /// Builds a list of bark syllables from a message using the given voice profile.
    /// </summary>
    protected List<BarkSyllable> BuildSyllables(string message, float basePitch, CEBarkSpeechPrototype profile)
    {
        var result = new List<BarkSyllable>();
        var sentences = SplitSentences(message);
        var totalSyllables = 0;

        // CrystallEdge: scale interval inversely with pitch so higher-pitched voices speak faster
        var effectiveInterval = profile.SyllableInterval / Math.Clamp(basePitch, MinPitchScale, MaxPitchScale);
        // CrystallEdge end

        foreach (var sentence in sentences)
        {
            if (totalSyllables >= profile.MaxSyllables)
                break;

            var intonation = DetectIntonation(sentence);
            var text = sentence.TrimEnd('.', '!', '?', ' ');
            if (string.IsNullOrEmpty(text))
                continue;

            var sentenceSyllables = new List<BarkSyllable>();
            var charIndex = 0;

            while (charIndex < text.Length && totalSyllables < profile.MaxSyllables)
            {
                var ch = text[charIndex];

                if (ch == ' ')
                {
                    sentenceSyllables.Add(new BarkSyllable
                    {
                        IsPause = true,
                        Duration = effectiveInterval * profile.WordPauseMultiplier,
                    });
                    charIndex++;
                    continue;
                }

                if (ch == ',')
                {
                    sentenceSyllables.Add(new BarkSyllable
                    {
                        IsPause = true,
                        Duration = effectiveInterval * profile.CommaPauseMultiplier,
                    });
                    charIndex++;
                    continue;
                }

                if (!char.IsLetterOrDigit(ch))
                {
                    charIndex++;
                    continue;
                }

                var consumed = 0;
                var representativeChar = ch;
                while (consumed < profile.CharsPerSyllable && charIndex < text.Length)
                {
                    var c = text[charIndex];
                    if (c == ' ' || c == ',')
                        break;

                    if (char.IsLetterOrDigit(c))
                    {
                        if (consumed == 0)
                            representativeChar = c;
                        consumed++;
                    }

                    charIndex++;
                }

                var pitch = CalculatePitch(representativeChar, basePitch, profile);

                sentenceSyllables.Add(new BarkSyllable
                {
                    Pitch = pitch,
                    Duration = effectiveInterval,
                });

                totalSyllables++;
            }

            // Count voiced syllables for position calculation.
            var voicedCount = 0;
            foreach (var s in sentenceSyllables)
            {
                if (!s.IsPause)
                    voicedCount++;
            }

            var voicedIndex = 0;
            foreach (var s in sentenceSyllables)
            {
                if (s.IsPause)
                {
                    result.Add(s);
                    continue;
                }

                var position = voicedCount > 1 ? (float) voicedIndex / (voicedCount - 1) : 0f;
                var modified = s;

                modified.Sound = SelectSound(position, intonation, profile);
                modified.AudioParams = profile.AudioParams;
                modified.Pitch = ApplyIntonation(modified.Pitch, position, intonation, profile);

                if (intonation == Intonation.Exclaim)
                    modified.VolumeBoost = profile.ExclaimVolumeBoost;

                result.Add(modified);
                voicedIndex++;
            }

            // Sentence boundary pause.
            if (result.Count > 0)
            {
                result.Add(new BarkSyllable
                {
                    IsPause = true,
                    Duration = effectiveInterval * profile.CommaPauseMultiplier,
                });
            }
        }

        return result;
    }

    private static SoundSpecifier SelectSound(float position, Intonation intonation, CEBarkSpeechPrototype profile)
    {
        if (position >= 0.7f)
        {
            return intonation switch
            {
                Intonation.Question => profile.AskSound ?? profile.SaySound,
                Intonation.Exclaim => profile.ExclaimSound ?? profile.SaySound,
                _ => profile.SaySound,
            };
        }

        return profile.SaySound;
    }

    private float CalculatePitch(char ch, float basePitch, CEBarkSpeechPrototype profile)
    {
        ch = char.ToLowerInvariant(ch);

        float charFactor;
        if ("aeiouаеёиоуыэюя".Contains(ch))
        {
            charFactor = ch switch
            {
                'a' or 'а' => 0.0f,
                'o' or 'о' => 0.1f,
                'u' or 'у' => 0.15f,
                'e' or 'е' or 'э' => 0.25f,
                'i' or 'и' => 0.3f,
                'ы' => 0.05f,
                'ё' => 0.2f,
                'ю' => 0.12f,
                'я' => 0.28f,
                _ => 0.15f,
            };
        }
        else if (char.IsDigit(ch))
        {
            charFactor = (ch - '0') * 0.04f + 0.1f;
        }
        else
        {
            charFactor = ((ch - 'a' + 16) % 26) * 0.015f + 0.35f;
        }

        var pitchOffset = (charFactor - 0.25f) * 0.4f;
        var variation = (float) _random.NextGaussian(0, profile.PitchVariation);

        return basePitch + pitchOffset + variation;
    }

    private static float ApplyIntonation(
        float pitch,
        float position,
        Intonation intonation,
        CEBarkSpeechPrototype profile)
    {
        switch (intonation)
        {
            case Intonation.Question:
                if (position > 0.7f)
                {
                    var rise = (position - 0.7f) / 0.3f;
                    pitch += profile.QuestionPitchRise * rise;
                }
                break;

            case Intonation.Exclaim:
                pitch += profile.ExclaimPitchBoost;
                break;

            case Intonation.Declarative:
                if (position > 0.8f)
                {
                    var drop = (position - 0.8f) / 0.2f;
                    pitch -= profile.DeclarativePitchDrop * drop;
                }
                break;
        }

        return pitch;
    }

    private static List<string> SplitSentences(string message)
    {
        var sentences = new List<string>();
        var start = 0;

        for (var i = 0; i < message.Length; i++)
        {
            if (message[i] is '.' or '!' or '?')
            {
                sentences.Add(message[start..(i + 1)]);
                start = i + 1;
            }
        }

        if (start < message.Length)
            sentences.Add(message[start..]);

        return sentences;
    }

    private static Intonation DetectIntonation(string sentence)
    {
        var trimmed = sentence.TrimEnd();
        if (trimmed.Length == 0)
            return Intonation.Declarative;

        return trimmed[^1] switch
        {
            '?' => Intonation.Question,
            '!' => Intonation.Exclaim,
            _ => Intonation.Declarative,
        };
    }

    protected struct BarkSyllable
    {
        public bool IsPause;
        public float Pitch;
        public float Duration;
        public float VolumeBoost;
        public SoundSpecifier Sound;
        public AudioParams AudioParams;
    }

    protected enum Intonation : byte
    {
        Declarative,
        Question,
        Exclaim,
    }
}
