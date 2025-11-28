using Content.Shared._CE.Skill.Components;
using Content.Shared._CE.Skill.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Skill.Restrictions;

public sealed partial class NeedPrerequisite : CESkillRestriction
{
    [DataField(required: true)]
    public ProtoId<CESkillPrototype> Prerequisite = new();

    public override bool Check(IEntityManager entManager, EntityUid target)
    {
        if (!entManager.TryGetComponent<CESkillStorageComponent>(target, out var skillStorage))
            return false;

        var learned = skillStorage.LearnedSkills;
        return learned.Contains(Prerequisite);
    }

    public override string GetDescription(IEntityManager entManager, IPrototypeManager protoManager)
    {
        var skillSystem = entManager.System<CESharedSkillSystem>();

        return Loc.GetString("ce-skill-req-prerequisite", ("name", skillSystem.GetSkillName(Prerequisite)));
    }
}
