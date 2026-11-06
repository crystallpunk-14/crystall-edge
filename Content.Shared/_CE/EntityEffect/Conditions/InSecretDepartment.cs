using Content.Shared._CE.Roles;
using Content.Shared.Mind;
using Content.Shared.Roles;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.EntityEffect.Conditions;

/// <summary>
/// Passes when the entity's mind has a secret role (current or original) of <see cref="Department"/>.
/// Mind roles aren't networked, so the client always fails it and leaves the decision to the server.
/// </summary>
public sealed partial class InSecretDepartment : CEEntityConditionBase<InSecretDepartment>
{
    [DataField(required: true)]
    public ProtoId<CESecretDepartmentPrototype> Department;

    public override string GetDescription(IEntityManager entityManager, IPrototypeManager prototype)
    {
        var name = prototype.Resolve(Department, out var department) ? Loc.GetString(department.Name) : Department.Id;
        return Loc.GetString(Inverted ? "ce-in-secret-department-forbidden" : "ce-in-secret-department-required", ("department", name));
    }
}

public sealed partial class CEInSecretDepartmentConditionSystem : CEEntityConditionSystem<InSecretDepartment>
{
    [Dependency] private INetManager _net = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private SharedRoleSystem _role = default!;

    protected override void Condition(ref CEEntityConditionEvent<InSecretDepartment> args)
    {
        if (_net.IsClient)
            return;

        if (!_proto.Resolve(args.Condition.Department, out var department))
            return;

        if (!_mind.TryGetMind(args.Entity, out var mindId, out var mind) ||
            !_role.MindHasRole<CESecretRoleComponent>((mindId, mind), out var roleEnt))
            return;

        var role = roleEnt.Value.Comp2;
        args.Result = role.Role is { } current && department.Roles.Contains(current) ||
            role.OriginalRole is { } original && department.Roles.Contains(original);
    }
}
