using Content.Server._CE.Objectives.Target.Components;
using Content.Server._CE.Roles;
using Content.Shared._CE.Objectives.Components;
using Content.Shared._CE.Objectives.Target.Components;

namespace Content.Server._CE.Objectives.Target;

public sealed partial class CETargetExcludeDepartmentTargetsSystem : EntitySystem
{
    [Dependency] private CESecretRoleSelectionSystem _secretRoleSelection = default!;

    [SubscribeLocalEvent]
    private void OnValidateCandidate(Entity<CETargetExcludeDepartmentTargetsComponent> ent, ref CEValidateObjectiveTargetCandidateEvent args)
    {
        if (!_secretRoleSelection.TryGetObjectiveHolderDepartment(args.Holder.Owner, out var department))
            return;

        var query = EntityQueryEnumerator<CEObjectiveHolderComponent>();
        while (query.MoveNext(out var holderUid, out var holderComp))
        {
            if (!_secretRoleSelection.TryGetObjectiveHolderDepartment(holderUid, out var holderDepartment) ||
                holderDepartment.ID != department.ID)
            {
                continue;
            }

            foreach (var objective in holderComp.OwnedObjectives)
            {
                if (objective == ent.Owner ||
                    !TryComp<CETargetObjectiveComponent>(objective, out var targetComp) ||
                    targetComp.Target != args.Candidate)
                {
                    continue;
                }

                args.Invalidate();
                return;
            }
        }
    }
}
