using Content.Server._CE.Objectives;
using Content.Shared._CE.Roles;
using Content.Shared.CCVar;
using Content.Shared.GameTicking;
using Content.Shared.Mind;
using Content.Shared.Roles;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;

namespace Content.Server._CE.Achievements.Achievement;

/// <summary>
/// Grants <see cref="CESecretRolePrototype.VictoryAchievement"/> at round end to players who still
/// hold that secret role and have completed every objective they have.
/// </summary>
public sealed partial class CESecretRoleVictoryAchievementSystem : EntitySystem
{
    [Dependency] private CEAchievementsSystem _achievement = default!;
    [Dependency] private CEObjectiveSystem _objectives = default!;
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private SharedRoleSystem _role = default!;

    [SubscribeLocalEvent]
    private void OnRoundEnd(RoundEndMessageEvent ev)
    {
        // Don't award achievements in integration tests to avoid interfering with test cleanup
        if (_cfg.GetCVar(CCVars.DatabaseSynchronous))
            return;

        var query = EntityQueryEnumerator<MindComponent>();
        while (query.MoveNext(out var mindId, out var mind))
        {
            if (mind.OriginalOwnerUserId is not { } userId)
                continue;

            if (!_role.MindHasRole<CESecretRoleComponent>((mindId, mind), out var roleEnt) ||
                roleEnt.Value.Comp2.Role is not { } roleId ||
                !_proto.TryIndex(roleId, out var role) ||
                role.VictoryAchievement is not { } achievement)
                continue;

            if (!AllObjectivesCompleted(mindId))
                continue;

            _ = _achievement.AddPlayerAchievementAsync(userId, achievement);
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
}
