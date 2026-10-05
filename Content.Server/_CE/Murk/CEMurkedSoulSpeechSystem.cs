using System.Text;
using Content.Server._CE.EntityEffect.Effects;
using Content.Server._CE.Murk.Components;
using Content.Shared._CE.Murk;
using Content.Shared.Speech;
using Robust.Shared.Random;

namespace Content.Server._CE.Murk;

/// <summary>
/// Makes up the phrases murked souls say: murk gibberish with an occasional word they overheard
/// or the name of someone nearby.
/// </summary>
public sealed partial class CEMurkedSoulSpeechSystem : EntitySystem
{
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private IRobustRandom _random = default!;

    [Dependency] private EntityQuery<CEMurkedSoulSpeechComponent> _soulQuery = default!;
    [Dependency] private EntityQuery<CEMurkedSoulSpeechAccentComponent> _accentQuery = default!;

    [SubscribeLocalEvent]
    private void OnListen(Entity<CEMurkedSoulSpeechComponent> ent, ref ListenEvent args)
    {
        // Souls don't learn from each other, otherwise they'd just echo gibberish around forever.
        if (args.Message.Contains('~') || _soulQuery.HasComponent(args.Source))
            return;

        foreach (var word in args.Message.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            var clean = word.Trim('.', ',', '!', '?', ';', ':', '"', '\'', '(', ')', '-', '~');
            if (clean.Length >= ent.Comp.MinWordLength)
                ent.Comp.HeardWords.Add(clean);
        }

        while (ent.Comp.HeardWords.Count > ent.Comp.MaxHeardWords)
        {
            ent.Comp.HeardWords.Remove(_random.Pick(new List<string>(ent.Comp.HeardWords)));
        }
    }

    [SubscribeLocalEvent]
    private void OnGeneratePhrase(Entity<CEMurkedSoulSpeechComponent> ent, ref CEGeneratePhraseEvent args)
    {
        args.Phrase ??= GeneratePhrase(ent.Comp, BuildContextWords(ent));
    }

    /// <summary>
    /// Strings together a few murk-gibberish words, occasionally dropping in something the soul
    /// actually heard - never more than a couple, it's still dead.
    /// </summary>
    private string GeneratePhrase(CEMurkedSoulSpeechComponent comp, HashSet<string> contextWords)
    {
        var wordCount = _random.Next(comp.MinWords, comp.MaxWords + 1);
        var words = new List<string>(wordCount);
        var contextList = new List<string>(contextWords);

        var maxRealWords = Math.Min(2, wordCount - 1);
        var realWordsUsed = 0;

        for (var i = 0; i < wordCount; i++)
        {
            var canUseReal = contextList.Count > 0 && realWordsUsed < maxRealWords;
            if (canUseReal && _random.Prob(comp.WordChance))
            {
                var picked = _random.Pick(contextList);
                contextList.Remove(picked);
                words.Add(picked);
                realWordsUsed++;
            }
            else
            {
                var len = _random.Next(2, 8);
                var sb = new StringBuilder(len);
                for (var j = 0; j < len; j++)
                    sb.Append(_random.Pick(CESharedMurkSystem.GibberishAlphabet));
                words.Add(sb.ToString());
            }
        }

        var phrase = string.Join(' ', words);
        var r = _random.NextFloat();
        if (r < 0.15f)
            phrase += "!";
        else if (r < 0.35f)
            phrase += "?";
        return phrase;
    }

    /// <summary>
    /// The soul's vocabulary: what it overheard, plus the names of anything nearby worth naming.
    /// </summary>
    private HashSet<string> BuildContextWords(Entity<CEMurkedSoulSpeechComponent> ent)
    {
        var words = new HashSet<string>(ent.Comp.HeardWords);

        foreach (var entity in _lookup.GetEntitiesInRange(ent, ent.Comp.AccentPickupRadius))
        {
            if (entity == ent.Owner)
                continue;

            if (_accentQuery.HasComponent(entity))
                words.Add(Name(entity));
        }

        return words;
    }
}
