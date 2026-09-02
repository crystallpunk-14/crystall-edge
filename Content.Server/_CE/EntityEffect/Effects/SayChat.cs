using Content.Server.Chat.Systems;
using Content.Shared._CE.EntityEffect;
using Content.Shared._CE.EntityEffect.Effects;
using Content.Shared.Speech;
using Content.Shared.Speech.Muting;
using Content.Shared.StatusEffectNew;

namespace Content.Server._CE.EntityEffect.Effects;

public sealed partial class CESayChatEffectSystem : CEEntityEffectSystem<SayChat>
{
    [Dependency] private ChatSystem _chat = default!;
    [Dependency] private StatusEffectsSystem _statusEffects = default!;

    protected override void Effect(ref CEEntityEffectEvent<SayChat> args)
    {
        if (ResolveEffectEntity(args.Args, args.Effect.EffectTarget) is not { } entity)
            return;

        if (!HasComp<SpeechComponent>(entity) || _statusEffects.HasEffectComp<MutedStatusEffectComponent>(entity))
            return;
        if (string.IsNullOrWhiteSpace(args.Effect.Sentence))
            return;

        _chat.TrySendInGameICMessage(entity, Loc.GetString(args.Effect.Sentence), args.Effect.ChatType, true);
    }
}
