using Content.Server.Chat.Systems;
using Content.Shared._CE.EntityEffect;
using Content.Shared._CE.EntityEffect.Effects;

namespace Content.Server._CE.EntityEffect.Effects;

public sealed partial class CESayGeneratedPhraseEffectSystem : CEEntityEffectSystem<SayGeneratedPhrase>
{
    [Dependency] private ChatSystem _chat = default!;

    protected override void Effect(ref CEEntityEffectEvent<SayGeneratedPhrase> args)
    {
        if (ResolveEffectEntity(args.Args, args.Effect.EffectTarget) is not { } speaker)
            return;

        var addressee = args.Args.Target == speaker ? null : args.Args.Target;
        var ev = new CEGeneratePhraseEvent(addressee);
        RaiseLocalEvent(speaker, ref ev);

        if (string.IsNullOrWhiteSpace(ev.Phrase))
            return;

        _chat.TrySendInGameICMessage(speaker, ev.Phrase, args.Effect.ChatType, args.Effect.Range);
    }
}

/// <summary>
/// Raised on an entity to get a phrase for it to say.
/// </summary>
[ByRefEvent]
public record struct CEGeneratePhraseEvent(EntityUid? Addressee)
{
    public string? Phrase;
}
