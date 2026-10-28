using Content.Server._CE.Statistics;
using Content.Shared._CE.Achievements.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server._CE.Achievements.Achievement;

/// <summary>
/// Grants <see cref="Achievement"/> once the player has finished enough rounds alive.
/// </summary>
public sealed partial class CERoundsFinishedAliveAchievementSystem : EntitySystem
{
    [Dependency] private CEAchievementsSystem _achievement = default!;

    private static readonly ProtoId<CEAchievementPrototype> Achievement = "RoundsFinishedAlive3";
    private const long RequiredRounds = 3;

    [SubscribeLocalEvent]
    private void OnStatisticUpdated(CEStatisticUpdatedEvent ev)
    {
        if (ev.GetTotalValue(CEStatistics.RoundsFinishedAlive) < RequiredRounds)
            return;

        if (_achievement.HasCachedAchievement(ev.UserId, Achievement))
            return;

        _ = _achievement.AddPlayerAchievementAsync(ev.UserId, Achievement);
    }
}
