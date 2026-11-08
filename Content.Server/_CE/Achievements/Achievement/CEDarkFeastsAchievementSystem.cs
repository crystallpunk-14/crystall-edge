using Content.Server._CE.Statistics;
using Content.Shared._CE.Achievements.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server._CE.Achievements.Achievement;

/// <summary>
/// Grants achievements once the player has held enough dark feasts as a werewolf.
/// </summary>
public sealed partial class CEDarkFeastsAchievementSystem : EntitySystem
{
    [Dependency] private CEAchievementsSystem _achievement = default!;

    private static readonly (long Feasts, ProtoId<CEAchievementPrototype> Achievement)[] Thresholds =
    [
        (5, "DarkFeasts5"),
    ];

    [SubscribeLocalEvent]
    private void OnStatisticUpdated(CEStatisticUpdatedEvent ev)
    {
        var feasts = ev.GetTotalValue(CEStatistics.DarkFeasts);
        foreach (var (required, achievement) in Thresholds)
        {
            if (feasts < required || _achievement.HasCachedAchievement(ev.UserId, achievement))
                continue;

            _ = _achievement.AddPlayerAchievementAsync(ev.UserId, achievement);
        }
    }
}
