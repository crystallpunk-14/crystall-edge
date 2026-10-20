using Content.Server._CE.Objectives.Target.Components;
using Content.Server._CE.Roles;
using Content.Server.Roles;
using Content.Shared._CE.Objectives.Target.Components;
using Content.Shared._CE.Roles;
using Content.Shared.Mind;

namespace Content.Server._CE.Objectives.Target;

public sealed partial class CETargetSecretDepartmentBlacklistSystem : EntitySystem
{
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private RoleSystem _role = default!;
    [Dependency] private CESecretRoleSelectionSystem _secretRoleSelection = default!;

    [SubscribeLocalEvent]
    private void OnValidateCandidate(Entity<CETargetSecretDepartmentBlacklistComponent> ent, ref CEValidateObjectiveTargetCandidateEvent args)
    {
        if (!_mind.TryGetMind(args.Candidate, out var candidateMindId, out _))
            return;

        if (_role.MindHasRole<CESecretRoleComponent>(candidateMindId, out var roleEnt) &&
            roleEnt.Value.Comp2.Role is { } roleId &&
            _secretRoleSelection.TryGetDepartment(roleId, out var department) &&
            ent.Comp.Departments.Contains(department.ID))
        {
            args.Invalidate();
        }
    }
}
