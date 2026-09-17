using Content.Server._CE.Objectives;
using Content.Shared._CE.Objectives.Components;
using Content.Shared._CE.Roles;
using Content.Shared.Mind;
using Content.Shared.Random.Helpers;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server._CE.Roles;

public sealed partial class CESecretRoleSelectionSystem
{
    [Dependency] private CEObjectiveSystem _objectives = default!;

    [SubscribeLocalEvent]
    private void OnGetAdditionalObjectives(Entity<MindComponent> ent, ref CEGetAdditionalObjectivesEvent args)
    {
        if (!_role.MindHasRole<CESecretRoleComponent>((ent.Owner, ent.Comp), out var roleEnt))
            return;

        if (roleEnt.Value.Comp2.Role is not { } roleId || !TryGetDepartment(roleId, out var department))
            return;

        var rules = QueryActiveRules();
        while (rules.MoveNext(out _, out _, out var comp, out _))
        {
            if (!comp.DepartmentObjectiveHolders.TryGetValue(department.ID, out var holderUid))
                continue;

            args.Objectives.AddRange(_objectives.GetObjectives(holderUid));
            return;
        }
    }

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

        if (TryGetDepartment(role.ID, out var department))
        {
            var departmentPool = overrides is { } d && d.DepartmentOverrides.TryGetValue(department.ID, out var deptOverride)
                ? deptOverride
                : department.ObjectivePool;

            if (departmentPool is not null)
                EnsureSharedDepartmentObjectives(rule, department, departmentPool);
        }

        EnsureComp<CEObjectiveHolderComponent>(mindId);
        _objectives.RegenerateObjectiveList(mindId);
    }

    private void EnsureSharedDepartmentObjectives(
        Entity<CESecretRoleSelectionComponent> rule,
        CESecretDepartmentPrototype department,
        CEObjectivePool pool)
    {
        if (rule.Comp.DepartmentObjectiveHolders.ContainsKey(department.ID))
            return;

        var holderUid = Spawn(null, MapCoordinates.Nullspace);
        rule.Comp.DepartmentObjectiveHolders[department.ID] = holderUid;
        CreateObjectivesFromPool(holderUid, pool, department.Name, department.Color);
    }

    private List<EntityUid> CreateObjectivesFromPool(
        EntityUid holderUid,
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

            if (!_objectives.TryCreateObjective(holderUid, objectiveProto, out var objective))
                continue;

            if (descriptorName is { } name)
                _objectives.SetDescriptor(objective.Value.Owner, name, color ?? Color.White);

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
