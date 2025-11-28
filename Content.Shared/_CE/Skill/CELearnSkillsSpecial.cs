using Content.Shared._CE.Skill.Prototypes;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Skill;

public sealed partial class CELearnSkillsSpecial : JobSpecial
{
    [DataField]
    public HashSet<ProtoId<CESkillPrototype>> Skills { get; private set; } = new();

    public override void AfterEquip(EntityUid mob)
    {
        var entMan = IoCManager.Resolve<IEntityManager>();
        var skillSys = entMan.System<CESharedSkillSystem>();

        foreach (var skill in Skills)
        {
            skillSys.TryAddSkill(mob, skill, free: true);
        }
    }
}
