using Content.Shared._CE.Roles;
using Content.Shared._CE.Skill;
using Content.Shared._CE.Skill.Components;
using Content.Shared._CE.Skill.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server._CE.Roles;

public sealed partial class CESecretRoleSelectionSystem
{
    [Dependency] private CESharedSkillSystem _skill = default!;

    private void GrantSecretRoleSkills(EntityUid target, CESecretRolePrototype role)
    {
        var hasDepartment = TryGetDepartment(role.ID, out var department);

        var roleDescriptor = new CESkillDescriptor { Name = role.Name, Color = hasDepartment ? department.Color : Color.White };
        foreach (var skill in role.Skills)
            _skill.TryAddSkill(target, skill, force: true, descriptor: roleDescriptor);

        if (!hasDepartment)
            return;

        var departmentDescriptor = new CESkillDescriptor { Name = department.Name, Color = department.Color };
        foreach (var skill in department.Skills)
            _skill.TryAddSkill(target, skill, force: true, descriptor: departmentDescriptor);
    }

    private void RemoveSecretRoleSkills(EntityUid target, ProtoId<CESecretRolePrototype> roleId)
    {
        if (!_proto.TryIndex(roleId, out var role))
            return;

        foreach (var skill in role.Skills)
            _skill.TryRemoveSkill(target, skill);

        if (TryGetDepartment(roleId, out var department))
        {
            foreach (var skill in department.Skills)
                _skill.TryRemoveSkill(target, skill);
        }
    }
}
