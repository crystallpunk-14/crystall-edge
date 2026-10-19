using Content.Server._CE.Murk;
using Content.Shared._CE.EntityEffect;
using Content.Shared._CE.EntityEffect.Effects;

namespace Content.Server._CE.EntityEffect.Effects;

public sealed partial class CEStartMurkConversationEffectSystem : CEEntityEffectSystem<StartMurkConversation>
{
    [Dependency] private CEMurkConversationSystem _conversation = default!;

    protected override void Effect(ref CEEntityEffectEvent<StartMurkConversation> args)
    {
        if (ResolveEffectEntity(args.Args, args.Effect.EffectTarget) is not { } target)
            return;

        var user = args.Args.Source;
        var effect = args.Effect;
        if (!_conversation.TryStart(user, target, effect.Lines, effect.MinTurnDelay, effect.MaxTurnDelay, effect.TurnTimeout, effect.MaxDistance))
            return;

        _conversation.FaceTo(user, target);
    }
}

public sealed partial class CEPassMurkConversationTurnEffectSystem : CEEntityEffectSystem<PassMurkConversationTurn>
{
    [Dependency] private CEMurkConversationSystem _conversation = default!;

    protected override void Effect(ref CEEntityEffectEvent<PassMurkConversationTurn> args)
    {
        if (ResolveEffectEntity(args.Args, args.Effect.EffectTarget) is not { } user)
            return;

        _conversation.FacePartner(user);
        _conversation.PassTurn(user);
    }
}
