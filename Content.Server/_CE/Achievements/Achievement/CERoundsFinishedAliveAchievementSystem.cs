using Content.Server._CE.Statistics;
using Content.Shared._CE.Achievements.Prototypes;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Server._CE.Achievements.Achievement;

/// <summary>
/// Grants achievements once the player has finished enough rounds alive, overall or within a department.
/// </summary>
public sealed partial class CERoundsFinishedAliveAchievementSystem : EntitySystem
{
    [Dependency] private CEAchievementsSystem _achievement = default!;

    private static readonly (long Rounds, ProtoId<CEAchievementPrototype> Achievement)[] OverallThresholds =
    [
        (1, "RoundsFinishedAlive1"),
        (2, "RoundsFinishedAlive2"),
        (3, "RoundsFinishedAlive3"),
    ];

    private static readonly (ProtoId<DepartmentPrototype> Department, long Rounds, ProtoId<CEAchievementPrototype> Achievement)[] DepartmentThresholds =
    [
        ("CEEngineering", 2, "RoundsFinishedAliveEngineering2"),
        ("CEGuild", 2, "RoundsFinishedAliveGuild2"),
        ("CETavern", 2, "RoundsFinishedAliveTavern2"),
    ];

    [SubscribeLocalEvent]
    private void OnStatisticUpdated(CEStatisticUpdatedEvent ev)
    {
        var rounds = ev.GetTotalValue(CEStatistics.RoundsFinishedAlive);
        foreach (var (required, achievement) in OverallThresholds)
        {
            if (rounds >= required)
                TryGrant(ev, achievement);
        }

        foreach (var (department, required, achievement) in DepartmentThresholds)
        {
            if (ev.GetTotalValue(CEStatistics.RoundsFinishedAliveDepartment, department) >= required)
                TryGrant(ev, achievement);
        }
    }

    private void TryGrant(CEStatisticUpdatedEvent ev, ProtoId<CEAchievementPrototype> achievement)
    {
        if (_achievement.HasCachedAchievement(ev.UserId, achievement))
            return;

        _ = _achievement.AddPlayerAchievementAsync(ev.UserId, achievement);
    }
}
