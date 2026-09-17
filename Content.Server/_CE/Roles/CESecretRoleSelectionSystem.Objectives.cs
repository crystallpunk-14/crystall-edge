using Content.Server._CE.Objectives;
using Content.Shared._CE.Objectives.Components;
using Content.Shared._CE.Roles;
using Content.Shared.Mind;
using Content.Shared.Random.Helpers;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server._CE.Roles;

// Objective distribution for secret roles: granted right alongside the role itself in
// GrantSecretRole, so round-start assignment and late-join both flow through one place.
// Uses our own CEObjectiveSystem (see Content.Shared._CE.Objectives), not upstream's Objectives
// system - see CEObjectiveComponent for why.
public sealed partial class CESecretRoleSelectionSystem
{
    [Dependency] private CEObjectiveSystem _ceObjectives = default!;

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
            CreateObjectivesFromPool(mindId, rolePool);

        if (!TryGetDepartment(role.ID, out var department))
            return;

        var departmentPool = overrides is { } d && d.DepartmentOverrides.TryGetValue(department.ID, out var deptOverride)
            ? deptOverride
            : department.ObjectivePool;

        if (departmentPool is not null)
            GrantSharedDepartmentObjectives(rule, mindId, department, departmentPool);
    }

    // Department objectives are shared across the whole faction: the pool is only drawn once
    // (for whoever is granted them first), and every other member of the department - including
    // late-joiners - is handed the same objective entities rather than a personal copy.
    private void GrantSharedDepartmentObjectives(
        Entity<CESecretRoleSelectionComponent> rule,
        EntityUid mindId,
        CESecretDepartmentPrototype department,
        CEObjectivePool pool)
    {
        if (rule.Comp.DepartmentObjectives.TryGetValue(department.ID, out var objectives))
        {
            var holderComp = EnsureComp<CEObjectiveHolderComponent>(mindId);
            foreach (var objective in objectives)
                _ceObjectives.AddObjective(mindId, holderComp, objective);
            return;
        }

        rule.Comp.DepartmentObjectives[department.ID] = CreateObjectivesFromPool(mindId, pool, department.Name, department.Color);
    }

    private List<EntityUid> CreateObjectivesFromPool(
        EntityUid mindId,
        CEObjectivePool pool,
        LocId? descriptorName = null,
        Color? color = null)
    {
        var created = new List<EntityUid>();
        var candidates = pool.Weighted.ShallowClone();
        var difficulty = 0f;

        while (difficulty < pool.MaxDifficulty && _random.TryPickAndTake(candidates, out var objectiveProto))
        {
            if (!_proto.Index(objectiveProto).TryComp<CEObjectiveComponent>(out var objectiveComp, EntityManager.ComponentFactory))
                continue;

            if (objectiveComp.Difficulty > pool.MaxDifficulty - difficulty)
                continue;

            if (!_ceObjectives.TryCreateObjective(mindId, objectiveProto, out var objective))
                continue;

            if (descriptorName is { } name)
                _ceObjectives.SetDescriptor(objective.Value.Owner, name, color ?? Color.White);

            difficulty += objectiveComp.Difficulty;
            created.Add(objective.Value.Owner);
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
