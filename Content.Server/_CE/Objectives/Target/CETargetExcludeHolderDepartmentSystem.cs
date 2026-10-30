using Content.Server._CE.Objectives.Target.Components;
using Content.Server._CE.Roles;
using Content.Shared._CE.Objectives.Target.Components;
using Content.Shared.Mind;

namespace Content.Server._CE.Objectives.Target;

public sealed partial class CETargetExcludeHolderDepartmentSystem : EntitySystem
{
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private CESecretRoleSelectionSystem _secretRoleSelection = default!;

    [SubscribeLocalEvent]
    private void OnValidateCandidate(Entity<CETargetExcludeHolderDepartmentComponent> ent, ref CEValidateObjectiveTargetCandidateEvent args)
    {
        if (!_secretRoleSelection.TryGetObjectiveHolderDepartment(args.Holder.Owner, out var holderDepartment))
            return;

        if (!_mind.TryGetMind(args.Candidate, out var candidateMindId, out _))
            return;

        if (_secretRoleSelection.TryGetMindDepartment(candidateMindId, out var candidateDepartment) && candidateDepartment.ID == holderDepartment.ID)
            args.Invalidate();
    }
}
