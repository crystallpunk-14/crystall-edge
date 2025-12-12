using Content.Shared._CE.Skill;
using Content.Shared._CE.Skill.Components;

namespace Content.Server._CE.Skill;

public sealed partial class CESkillSystem : CESharedSkillSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<CETryLearnSkillMessage>(OnClientRequestLearnSkill);
    }

    private void OnClientRequestLearnSkill(CETryLearnSkillMessage ev, EntitySessionEventArgs args)
    {
        var entity = GetEntity(ev.Entity);

        if (args.SenderSession.AttachedEntity != entity)
            return;

        TryLearnSkill(entity, ev.Skill);
    }
}
