using Content.Server._CE.Objectives.Target.Components;
using Content.Server._CE.Roles;
using Content.Server.Roles;
using Content.Shared._CE.Objectives.Target.Components;
using Content.Shared._CE.Roles;
using Content.Shared.Mind;

namespace Content.Server._CE.Objectives.Target;

/// <summary>
/// Handles <see cref="CETargetExcludeHolderDepartmentComponent"/>.
/// </summary>
public sealed partial class CETargetExcludeHolderDepartmentSystem : EntitySystem
{
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private RoleSystem _role = default!;
    [Dependency] private CESecretRoleSelectionSystem _secretRoleSelection = default!;

    [SubscribeLocalEvent]
    private void OnValidateCandidate(Entity<CETargetExcludeHolderDepartmentComponent> ent, ref CEValidateObjectiveTargetCandidateEvent args)
    {
        if (!TryGetDepartment(args.Holder.Owner, out var holderDepartment))
            return;

        if (!_mind.TryGetMind(args.Candidate, out var candidateMindId, out _))
            return;

        if (TryGetDepartment(candidateMindId, out var candidateDepartment) && candidateDepartment.ID == holderDepartment.ID)
            args.Invalidate();
    }

    private bool TryGetDepartment(EntityUid mindId, out CESecretDepartmentPrototype department)
    {
        department = default!;

        return _role.MindHasRole<CESecretRoleComponent>(mindId, out var roleEnt) &&
               roleEnt.Value.Comp2.Role is { } roleId &&
               _secretRoleSelection.TryGetDepartment(roleId, out department);
    }
}
