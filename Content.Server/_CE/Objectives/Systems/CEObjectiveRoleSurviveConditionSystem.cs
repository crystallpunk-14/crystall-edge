using Content.Server._CE.Objectives.Components;
using Content.Shared._CE.Objectives.Components;
using Content.Shared._CE.Roles;
using Content.Shared.Mind;
using Content.Shared.Mobs;
using Content.Shared.Roles;

namespace Content.Server._CE.Objectives.Systems;

/// <summary>
/// Handles progress for <see cref="CEObjectiveRoleSurviveConditionComponent"/> - the average
/// survival of every holder of the listed roles: alive = 1, critical = 0.5, dead or gone = 0.
/// </summary>
public sealed partial class CEObjectiveRoleSurviveConditionSystem : EntitySystem
{
    [Dependency] private CEObjectiveSystem _objectives = default!;
    [Dependency] private CEObjectiveDepartmentSurviveConditionSystem _departmentSurvive = default!;
    [Dependency] private SharedRoleSystem _role = default!;

    [SubscribeLocalEvent]
    private void OnGetProgress(Entity<CEObjectiveRoleSurviveConditionComponent> ent, ref CEGetObjectiveProgressEvent args)
    {
        var holders = 0;
        var survival = 0f;

        var query = EntityQueryEnumerator<MindComponent>();
        while (query.MoveNext(out var mindId, out var mind))
        {
            if (!_role.MindHasRole<CESecretRoleComponent>((mindId, mind), out var roleEnt) ||
                roleEnt.Value.Comp2.Role is not { } roleId ||
                !ent.Comp.Roles.Contains(roleId))
            {
                continue;
            }

            holders++;
            survival += _departmentSurvive.GetSurvival(mind);
        }

        if (holders == 0)
        {
            args.Progress = ent.Comp.Invert ? 1f : 0f;
            return;
        }

        var average = survival / holders;
        args.Progress = ent.Comp.Invert ? 1f - average : average;
    }

    [SubscribeLocalEvent]
    private void OnMobStateChanged(MobStateChangedEvent args)
    {
        var query = EntityQueryEnumerator<CEObjectiveRoleSurviveConditionComponent>();
        while (query.MoveNext(out var uid, out _))
        {
            _objectives.RefreshObjectiveProgress(uid);
        }
    }
}
