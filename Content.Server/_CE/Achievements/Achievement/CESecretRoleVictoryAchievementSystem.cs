using Content.Server._CE.Objectives;
using Content.Server._CE.Roles;
using Content.Shared._CE.Roles;
using Content.Shared.CCVar;
using Content.Shared.GameTicking;
using Content.Shared.Mind;
using Content.Shared.Roles;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;

namespace Content.Server._CE.Achievements.Achievement;

/// <summary>
/// Grants victory achievements at round end:
/// <see cref="CESecretRolePrototype.VictoryAchievement"/> to players who still hold that secret role
/// and have completed every objective they have;
/// <see cref="CESecretDepartmentPrototype.VictoryAchievement"/> to every member of a department
/// whose shared objectives are all completed.
/// </summary>
public sealed partial class CESecretRoleVictoryAchievementSystem : EntitySystem
{
    [Dependency] private CEAchievementsSystem _achievement = default!;
    [Dependency] private CEObjectiveSystem _objectives = default!;
    [Dependency] private CESecretRoleSelectionSystem _secretRoles = default!;
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private SharedRoleSystem _role = default!;

    [SubscribeLocalEvent]
    private void OnRoundEnd(RoundEndMessageEvent ev)
    {
        // Don't award achievements in integration tests to avoid interfering with test cleanup
        if (_cfg.GetCVar(CCVars.DatabaseSynchronous))
            return;

        var departmentWins = new Dictionary<string, bool>();

        var query = EntityQueryEnumerator<MindComponent>();
        while (query.MoveNext(out var mindId, out var mind))
        {
            if (mind.OriginalOwnerUserId is not { } userId)
                continue;

            if (!_role.MindHasRole<CESecretRoleComponent>((mindId, mind), out var roleEnt) ||
                roleEnt.Value.Comp2.Role is not { } roleId ||
                !_proto.TryIndex(roleId, out var role))
                continue;

            if (role.VictoryAchievement is { } achievement && AllObjectivesCompleted(mindId))
                _ = _achievement.AddPlayerAchievementAsync(userId, achievement);

            if (_secretRoles.TryGetDepartment(roleId, out var department) &&
                department.VictoryAchievement is { } departmentAchievement)
            {
                if (!departmentWins.TryGetValue(department.ID, out var won))
                {
                    won = DepartmentObjectivesCompleted(department);
                    departmentWins[department.ID] = won;
                }

                if (won)
                    _ = _achievement.AddPlayerAchievementAsync(userId, departmentAchievement);
            }
        }
    }

    private bool AllObjectivesCompleted(EntityUid mindId)
    {
        foreach (var objective in _objectives.GetObjectives(mindId))
        {
            _objectives.RefreshObjectiveProgress(objective.AsNullable());

            if (!_objectives.IsCompleted(objective.AsNullable()))
                return false;
        }

        return true;
    }

    private bool DepartmentObjectivesCompleted(CESecretDepartmentPrototype department)
    {
        if (!_secretRoles.TryGetDepartmentObjectiveHolder(department.ID, out var holderUid))
            return false;

        var any = false;
        foreach (var objective in _objectives.GetObjectives(holderUid))
        {
            any = true;
            _objectives.RefreshObjectiveProgress(objective.AsNullable());

            if (!_objectives.IsCompleted(objective.AsNullable()))
                return false;
        }

        return any;
    }
}
