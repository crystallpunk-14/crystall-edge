using Content.Server.Objectives;
using Content.Shared._CE.Roles;
using Content.Shared.Mind;
using Content.Shared.Objectives.Components;
using Content.Shared.Random.Helpers;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

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
            CreateObjectivesFromPool(mindId, mind, rolePool);

        if (!TryGetDepartment(role.ID, out var department))
            return;

        var departmentPool = overrides is { } d && d.DepartmentOverrides.TryGetValue(department.ID, out var deptOverride)
            ? deptOverride
            : department.ObjectivePool;

        if (departmentPool is not null)
            GrantSharedDepartmentObjectives(rule, mindId, mind, department.ID, departmentPool);
    }

    // Department objectives are shared across the whole faction: the pool is only drawn once
    // (for whoever is granted them first), and every other member of the department - including
    // late-joiners - is handed the same objective entities rather than a personal copy.
    private void GrantSharedDepartmentObjectives(
        Entity<CESecretRoleSelectionComponent> rule,
        EntityUid mindId,
        MindComponent mind,
        ProtoId<CESecretDepartmentPrototype> department,
        CEObjectivePool pool)
    {
        if (rule.Comp.DepartmentObjectives.TryGetValue(department, out var objectives))
        {
            foreach (var objective in objectives)
                _mind.AddObjective(mindId, mind, objective);
            return;
        }

        rule.Comp.DepartmentObjectives[department] = CreateObjectivesFromPool(mindId, mind, pool);
    }

    private List<EntityUid> CreateObjectivesFromPool(EntityUid mindId, MindComponent mind, CEObjectivePool pool)
    {
        var created = new List<EntityUid>();
        var candidates = pool.Weighted.ShallowClone();
        var difficulty = 0f;

        while (difficulty < pool.MaxDifficulty && _random.TryPickAndTake(candidates, out var objectiveProto))
        {
            if (!_proto.Index(objectiveProto).TryComp<ObjectiveComponent>(out var objectiveComp, EntityManager.ComponentFactory))
                continue;

            if (objectiveComp.Difficulty > pool.MaxDifficulty - difficulty)
                continue;

            if (!_objectives.TryCreateObjective((mindId, mind), objectiveProto, out var objective))
                continue;

            _mind.AddObjective(mindId, mind, objective.Value);
            difficulty += objectiveComp.Difficulty;
            created.Add(objective.Value);
        }

        return created;
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
