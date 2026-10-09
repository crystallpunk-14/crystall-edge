using Content.Server._CE.Objectives.Components;
using Content.Shared._CE.Objectives.Components;
using Content.Shared._CE.Roles;
using Content.Shared.Ghost.Components;
using Content.Shared.Mind;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Roles;
using Robust.Server.Player;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;

namespace Content.Server._CE.Objectives.Systems;

/// <summary>
/// Handles progress for <see cref="CEObjectiveDepartmentSurviveConditionComponent"/> - the average
/// of every department member's survival: alive = 1, critical = 0.5, dead or gone = 0.
/// </summary>
public sealed partial class CEObjectiveDepartmentSurviveConditionSystem : EntitySystem
{
    [Dependency] private CEObjectiveSystem _objectives = default!;
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private SharedRoleSystem _role = default!;

    [SubscribeLocalEvent]
    private void OnGetProgress(Entity<CEObjectiveDepartmentSurviveConditionComponent> ent, ref CEGetObjectiveProgressEvent args)
    {
        if (!_proto.TryIndex(ent.Comp.Department, out var department))
        {
            args.Progress = 0f;
            return;
        }

        var members = 0;
        var survival = 0f;

        var query = EntityQueryEnumerator<MindComponent>();
        while (query.MoveNext(out var mindId, out var mind))
        {
            if (!_role.MindHasRole<CESecretRoleComponent>((mindId, mind), out var roleEnt) ||
                roleEnt.Value.Comp2.Role is not { } roleId ||
                !department.Roles.Contains(roleId))
            {
                continue;
            }

            members++;
            survival += GetSurvival(mind);
        }

        if (members == 0)
        {
            args.Progress = ent.Comp.Invert ? 1f : 0f;
            return;
        }

        var average = survival / members;
        args.Progress = ent.Comp.Invert ? 1f - average : average;
    }

    [SubscribeLocalEvent]
    private void OnMobStateChanged(MobStateChangedEvent args)
    {
        var query = EntityQueryEnumerator<CEObjectiveDepartmentSurviveConditionComponent>();
        while (query.MoveNext(out var uid, out _))
        {
            _objectives.RefreshObjectiveProgress(uid);
        }
    }

    /// <summary>
    /// Survival of a mind's body: alive = 1, critical = 0.5, dead or gone = 0.
    /// </summary>
    public float GetSurvival(MindComponent mind)
    {
        // Ghosted out, left the server or lost the body entirely - counts as not having survived.
        if (mind.OwnedEntity is not { } body || HasComp<GhostComponent>(body))
            return 0f;

        if (mind.UserId is not { } userId ||
            !_player.TryGetSessionById(userId, out var session) ||
            session.Status != SessionStatus.InGame)
        {
            return 0f;
        }

        if (!TryComp<MobStateComponent>(body, out var mobState))
            return 1f;

        return mobState.CurrentState switch
        {
            MobState.Alive => 1f,
            MobState.Critical => 0.5f,
            MobState.Dead => 0f,
            _ => 1f,
        };
    }
}
