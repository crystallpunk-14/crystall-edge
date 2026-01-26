using Content.Server._CE.GameTicking.Components;
using Content.Server.Antag;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Shared._CE.Skill;
using Content.Shared.GameTicking.Components;
using Robust.Shared.Prototypes;

namespace Content.Server._CE.GameTicking;

public sealed class CEThiefRuleSystem : GameRuleSystem<CEThiefRuleComponent>
{
    [Dependency] private readonly CESharedSkillSystem _skill = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CEThiefRuleComponent, AfterAntagEntitySelectedEvent>(AfterAntagSelected);
    }

    private void AfterAntagSelected(Entity<CEThiefRuleComponent> ent, ref AfterAntagEntitySelectedEvent args)
    {
        _skill.AddSkillTree(args.EntityUid, ent.Comp.ThiefSkillTree);
    }
}
