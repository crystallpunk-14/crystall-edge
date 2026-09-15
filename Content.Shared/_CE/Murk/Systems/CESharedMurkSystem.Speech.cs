using System.Text;
using Content.Shared._CE.Murk.Components;
using Content.Shared.Speech;

namespace Content.Shared._CE.Murk.Systems;

public abstract partial class CESharedMurkSystem
{
    /// <summary>
    /// Dissolution level at which every word comes out as gibberish.
    /// </summary>
    private const float FullGibberishAt = 0.8f;

    private static readonly char[] GibberishAlphabet =
    [
        'ä', 'ã', 'ç', 'ø', 'ђ', 'œ', 'Ї', 'Ћ', 'ў', 'ž', 'ö', 'є', 'þ',
        'ï', 'ñ', 'ë', 'â', 'ô', 'û', 'î', 'ê', 'ù', 'ü',
    ];

    [SubscribeLocalEvent]
    private void OnDissolvingAccent(Entity<CEMurkDissolvingComponent> ent, ref AccentGetEvent args)
    {
        if (!ent.Comp.Enabled || !ent.Comp.AffectSpeech)
            return;

        args.Message = Garble(args.Message, ent.Comp.Dissolved);
    }

    /// <summary>
    /// Replaces words with gibberish, each word having a <paramref name="dissolved"/>-scaled chance
    /// to be eaten. The randomness is seeded from the message itself, so repeating the same phrase
    /// always comes out the same way - the speaker can't reroll the dice by spamming.
    /// </summary>
    public string Garble(string message, float dissolved)
    {
        var chance = Math.Clamp(dissolved / FullGibberishAt, 0f, 1f);
        if (chance <= 0f)
            return message;

        var random = new System.Random(StableHash(message));
        var words = message.Split(' ');

        for (var i = 0; i < words.Length; i++)
        {
            if (random.NextDouble() >= chance)
                continue;

            words[i] = GarbleWord(words[i], random);
        }

        return string.Join(' ', words);
    }

    /// <summary>
    /// Swaps every letter and digit for a gibberish one, keeping the length and any punctuation
    /// in place so the message still reads like speech.
    /// </summary>
    private static string GarbleWord(string word, System.Random random)
    {
        var builder = new StringBuilder(word.Length);

        foreach (var character in word)
        {
            builder.Append(char.IsLetterOrDigit(character)
                ? GibberishAlphabet[random.Next(GibberishAlphabet.Length)]
                : character);
        }

        return builder.ToString();
    }

    /// <summary>
    /// FNV-1a. <see cref="string.GetHashCode()"/> is randomized per process, which would make the
    /// same phrase garble differently after every restart.
    /// </summary>
    private static int StableHash(string value)
    {
        unchecked
        {
            var hash = 2166136261;

            foreach (var character in value)
            {
                hash ^= character;
                hash *= 16777619;
            }

            return (int)hash;
        }
    }
}
