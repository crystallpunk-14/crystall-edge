using Content.Shared._CE.Skill.Components;
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
        AfterEquip(mob, null);
    }

    public override void AfterEquip(EntityUid mob, ProtoId<JobPrototype>? job)
    {
        var entMan = IoCManager.Resolve<IEntityManager>();
        var protoMan = IoCManager.Resolve<IPrototypeManager>();
        var skillSys = entMan.System<CESharedSkillSystem>();

        var descriptor = BuildProfessionDescriptor(job, protoMan);

        foreach (var skill in Skills)
        {
            skillSys.TryAddSkill(mob, skill, force: true, descriptor: descriptor);
        }
    }

    /// <summary>
    /// Badges the skill with the granting job's name, colored by its primary department -
    /// mirrors the job-color lookup the character menu already does for the job label itself.
    /// </summary>
    private static CESkillDescriptor? BuildProfessionDescriptor(ProtoId<JobPrototype>? job, IPrototypeManager protoMan)
    {
        if (job is not { } jobId || !protoMan.TryIndex(jobId, out var jobProto))
            return null;

        DepartmentPrototype? matchedDepartment = null;
        foreach (var department in protoMan.EnumeratePrototypes<DepartmentPrototype>())
        {
            if (!department.Roles.Contains(jobId))
                continue;

            matchedDepartment = department;
            if (department.Primary)
                break;
        }

        return new CESkillDescriptor
        {
            Name = jobProto.Name,
            Color = matchedDepartment?.Color ?? Color.White,
        };
    }
}
