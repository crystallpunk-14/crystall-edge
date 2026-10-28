using Content.Server._CE.Statistics;
using Content.Shared._CE.Achievements.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server._CE.Achievements.Achievement;

/// <summary>
/// Grants achievements once the player has finished enough rounds alive.
/// </summary>
public sealed partial class CERoundsFinishedAliveAchievementSystem : EntitySystem
{
    [Dependency] private CEAchievementsSystem _achievement = default!;

    private static readonly (long Rounds, ProtoId<CEAchievementPrototype> Achievement)[] Thresholds =
    [
        (1, "RoundsFinishedAlive1"),
        (3, "RoundsFinishedAlive3"),
    ];

    [SubscribeLocalEvent]
    private void OnStatisticUpdated(CEStatisticUpdatedEvent ev)
    {
        var rounds = ev.GetTotalValue(CEStatistics.RoundsFinishedAlive);

        foreach (var (required, achievement) in Thresholds)
        {
            if (rounds < required)
                continue;

            if (_achievement.HasCachedAchievement(ev.UserId, achievement))
                continue;

            _ = _achievement.AddPlayerAchievementAsync(ev.UserId, achievement);
        }
    }
}
