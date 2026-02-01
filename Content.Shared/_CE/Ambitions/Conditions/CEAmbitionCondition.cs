using Content.Shared._CE.Roles;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Mind;
using Content.Shared.Roles;
using Content.Shared.Roles.Jobs;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Ambitions.Conditions;

[ImplicitDataDefinitionForInheritors]
[MeansImplicitUse]
public abstract partial class CEAmbitionCondition
{
    [DataField]
    public bool Inverted = false;

    public abstract bool Check(IEntityManager entManager, IPrototypeManager protoManager, EntityUid owner);
}

public sealed partial class RequiredSpecies : CEAmbitionCondition
{
    [DataField(required: true)]
    public HashSet<ProtoId<SpeciesPrototype>> Species = new();

    public override bool Check(IEntityManager entManager, IPrototypeManager protoManager, EntityUid owner)
    {
        if (!entManager.TryGetComponent(owner, out HumanoidAppearanceComponent? humanoid))
            return false;

        if (Inverted)
            return !Species.Contains(humanoid.Species);

        return Species.Contains(humanoid.Species);
    }
}

public sealed partial class RequiredJob : CEAmbitionCondition
{
    [DataField(required: true)]
    public HashSet<ProtoId<JobPrototype>> Jobs = new();

    public override bool Check(IEntityManager entManager, IPrototypeManager protoManager, EntityUid owner)
    {
        var mindSystem = entManager.System<SharedMindSystem>();
        var jobSystem = entManager.System<SharedJobSystem>();

        if (!mindSystem.TryGetMind(owner, out var mindId, out var mind))
            return false;

        if (!jobSystem.MindTryGetJob(mindId, out var jobProto))
            return false;

        if (Inverted)
            return !Jobs.Contains(jobProto);

        return Jobs.Contains(jobProto);
    }
}

public sealed partial class RequiredDepartment : CEAmbitionCondition
{
    [DataField(required: true)]
    public HashSet<ProtoId<DepartmentPrototype>> Departments = new();

    public override bool Check(IEntityManager entManager, IPrototypeManager protoManager, EntityUid owner)
    {
        var mindSystem = entManager.System<SharedMindSystem>();
        var jobSystem = entManager.System<SharedJobSystem>();

        if (!mindSystem.TryGetMind(owner, out var mindId, out var mind))
            return false;

        if (!jobSystem.MindTryGetJob(mindId, out var jobProto))
            return false;

        var matched = false;

        foreach (var deptId in Departments)
        {
            if (!protoManager.Resolve(deptId, out var department))
                continue;

            if (!department.Roles.Contains(jobProto))
                continue;

            matched = true;
            break;
        }

        if (Inverted)
            return !matched;

        return matched;
    }
}

public sealed partial class RequiredThiefRole : CEAmbitionCondition
{
    public override bool Check(IEntityManager entManager, IPrototypeManager protoManager, EntityUid owner)
    {
        var mindSystem = entManager.System<SharedMindSystem>();

        if (!mindSystem.TryGetMind(owner, out var mindId, out var mind))
            return false;

        var matched = false;

        foreach (var roleId in mind.MindRoleContainer.ContainedEntities)
        {
            if (entManager.HasComponent<CEThiefRoleComponent>(roleId))
            {
                matched = true;
                break;
            }
        }

        if (Inverted)
            return !matched;

        return matched;
    }
}
