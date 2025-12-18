using Content.Server._CE.GameTicking.Components;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Shared.GameTicking.Components;
using Robust.Shared.Prototypes;

namespace Content.Server._CE.GameTicking;

public sealed class CEThiefRuleSystem : GameRuleSystem<CEThiefRuleComponent>
{
    [Dependency] private readonly IPrototypeManager _proto = default!;

    protected override void AppendRoundEndText(EntityUid uid,
        CEThiefRuleComponent component,
        GameRuleComponent gameRule,
        ref RoundEndTextAppendEvent args)
    {
        base.AppendRoundEndText(uid, component, gameRule, ref args);
    }
}
