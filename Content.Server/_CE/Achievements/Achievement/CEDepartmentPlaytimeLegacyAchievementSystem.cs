using Content.Server.Players.PlayTimeTracking;
using Content.Shared._CE.Achievements.Prototypes;
using Content.Shared.CCVar;
using Content.Shared.Roles;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server._CE.Achievements.Achievement;

/// <summary>
/// TEMPORARY: grants the department head achievements to players who already met the old department
/// playtime requirements, so they keep access to the head jobs. Delete this file to stop it.
/// </summary>
public sealed partial class CEDepartmentPlaytimeLegacyAchievementSystem : EntitySystem
{
    [Dependency] private CEAchievementsSystem _achievement = default!;
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private PlayTimeTrackingManager _playTime = default!;
    [Dependency] private IPrototypeManager _proto = default!;

    private static readonly (ProtoId<DepartmentPrototype> Department, TimeSpan Time, ProtoId<CEAchievementPrototype> Achievement)[] LegacyRequirements =
    [
        ("CEEngineering", TimeSpan.FromHours(4), "RoundsFinishedAliveEngineering2"),
        ("CEGuild", TimeSpan.FromHours(2.5), "RoundsFinishedAliveGuild2"),
        ("CETavern", TimeSpan.FromHours(2.5), "RoundsFinishedAliveTavern2"),
    ];

    public override void Initialize()
    {
        base.Initialize();

        _playTime.SessionPlayTimeUpdated += OnSessionPlayTimeUpdated;
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _playTime.SessionPlayTimeUpdated -= OnSessionPlayTimeUpdated;
    }

    private void OnSessionPlayTimeUpdated(ICommonSession session)
    {
        if (_cfg.GetCVar(CCVars.DatabaseSynchronous))
            return;

        if (!_playTime.TryGetTrackerTimes(session, out var playTimes))
            return;

        foreach (var (departmentId, required, achievement) in LegacyRequirements)
        {
            if (_achievement.HasCachedAchievement(session.UserId, achievement))
                continue;

            if (!_proto.TryIndex(departmentId, out var department))
                continue;

            var total = TimeSpan.Zero;
            foreach (var job in department.Roles)
            {
                if (_proto.TryIndex(job, out var jobProto))
                    total += playTimes.GetValueOrDefault(jobProto.PlayTimeTracker);
            }

            if (total >= required)
                _ = _achievement.AddPlayerAchievementAsync(session.UserId, achievement);
        }
    }
}
