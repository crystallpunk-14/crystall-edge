using Content.Server._CE.Objectives;
using Content.Shared._CE.Objectives.Components;
using Content.Shared._CE.Roles;
using Content.Shared.EntityTable;
using Content.Shared.EntityTable.EntitySelectors;
using Content.Shared.Mind;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Server._CE.Roles;

public sealed partial class CESecretRoleSelectionSystem
{
    [Dependency] private CEObjectiveSystem _objectives = default!;
    [Dependency] private EntityTableSystem _entityTable = default!;

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

        var roleObjectives = overrides is { } o && o.RoleOverrides.TryGetValue(role.ID, out var roleOverride)
            ? roleOverride
            : role.Objectives;

        var hasDepartment = TryGetDepartment(role.ID, out var department);

        if (roleObjectives is not null)
            CreateObjectivesFromTable(mindId, roleObjectives, role.Name, hasDepartment ? department.Color : Color.White);

        if (hasDepartment)
        {
            var departmentObjectives = overrides is { } d && d.DepartmentOverrides.TryGetValue(department.ID, out var deptOverride)
                ? deptOverride
                : department.Objectives;

            if (departmentObjectives is not null)
                EnsureSharedDepartmentObjectives(rule, department, departmentObjectives);
        }

        EnsureComp<CEObjectiveHolderComponent>(mindId);
        _objectives.RegenerateObjectiveList(mindId);
    }

    private void EnsureSharedDepartmentObjectives(
        Entity<CESecretRoleSelectionComponent> rule,
        CESecretDepartmentPrototype department,
        EntityTableSelector table)
    {
        if (rule.Comp.DepartmentObjectiveHolders.ContainsKey(department.ID))
            return;

        var holderUid = Spawn(null, MapCoordinates.Nullspace);
        rule.Comp.DepartmentObjectiveHolders[department.ID] = holderUid;
        CreateObjectivesFromTable(holderUid, table, department.Name, department.Color);
    }

    private List<EntityUid> CreateObjectivesFromTable(
        EntityUid holderUid,
        EntityTableSelector table,
        LocId? descriptorName = null,
        Color? color = null)
    {
        var created = new List<EntityUid>();

        foreach (var objectiveProto in _entityTable.GetSpawns(table))
        {
            if (!_objectives.TryCreateObjective(holderUid, objectiveProto, out var objective))
                continue;

            if (descriptorName is { } name)
                _objectives.SetDescriptor(objective.Value.Owner, name, color ?? Color.White);

            created.Add(objective.Value.Owner);
        }

        return created;
    }

    public bool TryGetDepartment(ProtoId<CESecretRolePrototype> role, out CESecretDepartmentPrototype department)
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
