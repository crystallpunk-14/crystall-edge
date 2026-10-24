using Content.Server._CE.Objectives.Components;
using Content.Server._CE.Roles;
using Content.Shared._CE.Murk;
using Content.Shared._CE.Objectives.Components;
using Content.Shared.Mobs;

namespace Content.Server._CE.Objectives.Systems;

/// <summary>
/// Handles progress for <see cref="CEObjectiveRoleSurviveConditionComponent"/> - the average
/// survival of every body that held one of the listed roles: alive = 1, critical = 0.5, dead, dissolved or deleted = 0.
/// </summary>
public sealed partial class CEObjectiveRoleSurviveConditionSystem : EntitySystem
{
    [Dependency] private CEObjectiveSystem _objectives = default!;
    [Dependency] private CEObjectiveDepartmentSurviveConditionSystem _departmentSurvive = default!;

    [SubscribeLocalEvent]
    private void OnInitialize(Entity<CEObjectiveRoleSurviveConditionComponent> ent, ref CEInitializeObjectiveEvent args)
    {
        _departmentSurvive.CollectBodies(ent.Comp.Bodies, ent.Comp.Roles);
    }

    [SubscribeLocalEvent]
    private void OnRoleGranted(ref CESecretRoleGrantedEvent args)
    {
        var query = EntityQueryEnumerator<CEObjectiveRoleSurviveConditionComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (!_departmentSurvive.IsHolder(comp.Roles, args.Role, args.Original) || !comp.Bodies.Add(args.Body))
                continue;

            _objectives.RefreshObjectiveProgress(uid);
        }
    }

    [SubscribeLocalEvent]
    private void OnGetProgress(Entity<CEObjectiveRoleSurviveConditionComponent> ent, ref CEGetObjectiveProgressEvent args)
    {
        args.Progress = _departmentSurvive.GetProgress(ent.Comp.Bodies, ent.Comp.Invert);
    }

    [SubscribeLocalEvent]
    private void OnMobStateChanged(MobStateChangedEvent args)
    {
        RefreshAll();
    }

    [SubscribeLocalEvent]
    private void OnSoulConverted(ref CEMurkSoulConvertedEvent args)
    {
        RefreshAll();
    }

    private void RefreshAll()
    {
        var query = EntityQueryEnumerator<CEObjectiveRoleSurviveConditionComponent>();
        while (query.MoveNext(out var uid, out _))
        {
            _objectives.RefreshObjectiveProgress(uid);
        }
    }
}
