using Content.Server.Objectives;
using Content.Shared._CE.Roles;
using Content.Shared.Mind;
using Content.Shared.Objectives.Components;
using Robust.Shared.Prototypes;

namespace Content.Server._CE.Roles;

// Objective distribution for secret roles: granted right alongside the role itself in
// GrantSecretRole, so round-start assignment and late-join both flow through one place.
public sealed partial class CESecretRoleSelectionSystem
{
    [Dependency] private ObjectivesSystem _objectives = default!;

    private void GrantSecretRoleObjectives(
        Entity<CESecretRoleSelectionComponent> rule,
        EntityUid mindId,
        MindComponent mind,
        CESecretRolePrototype role)
    {
        var overrides = CompOrNull<CESecretRoleObjectivesOverrideComponent>(rule.Owner);

        var rolePool = overrides is { } o && o.RoleOverrides.TryGetValue(role.ID, out var roleOverride)
            ? roleOverride
            : role.ObjectivePool;

        if (rolePool is not null)
            GrantObjectivesFromPool(mindId, mind, rolePool);

        if (!TryGetDepartment(role.ID, out var department))
            return;

        var departmentPool = overrides is { } d && d.DepartmentOverrides.TryGetValue(department.ID, out var deptOverride)
            ? deptOverride
            : department.ObjectivePool;

        if (departmentPool is not null)
            GrantObjectivesFromPool(mindId, mind, departmentPool);
    }

    private void GrantObjectivesFromPool(EntityUid mindId, MindComponent mind, CEObjectivePool pool)
    {
        var difficulty = 0f;
        foreach (var set in pool.Sets)
        {
            if (!_random.Prob(set.Prob))
                continue;

            for (var pick = 0; pick < set.MaxPicks && pool.MaxDifficulty > difficulty; pick++)
            {
                var remainingDifficulty = pool.MaxDifficulty - difficulty;
                if (_objectives.GetRandomObjective(mindId, mind, set.Groups, remainingDifficulty) is not { } objective)
                    continue;

                _mind.AddObjective(mindId, mind, objective);
                difficulty += Comp<ObjectiveComponent>(objective).Difficulty;
            }
        }
    }

    private bool TryGetDepartment(ProtoId<CESecretRolePrototype> role, out CESecretDepartmentPrototype department)
    {
        foreach (var candidate in _proto.EnumeratePrototypes<CESecretDepartmentPrototype>())
        {
            if (!candidate.Roles.Contains(role))
                continue;

            department = candidate;
            return true;
        }

        department = default!;
        return false;
    }
}
