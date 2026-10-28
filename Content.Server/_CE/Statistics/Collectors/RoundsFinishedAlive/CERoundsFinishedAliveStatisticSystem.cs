using Content.Shared.GameTicking;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Network;

namespace Content.Server._CE.Statistics.Collectors.RoundsFinishedAlive;

/// <summary>
/// Counts <see cref="CEStatistics.RoundsFinishedAlive"/> for lobby characters that are not dead at round end.
/// </summary>
public sealed partial class CERoundsFinishedAliveStatisticSystem : EntitySystem
{
    [Dependency] private CEStatisticSystem _statistic = default!;
    [Dependency] private MobStateSystem _mobState = default!;

    [SubscribeLocalEvent]
    private void OnPlayerSpawnComplete(PlayerSpawnCompleteEvent ev)
    {
        var tracker = EnsureComp<CERoundsFinishedAliveTrackerComponent>(ev.Mob);
        tracker.Player = ev.Player.UserId;
        tracker.Job = ev.JobId;
    }

    [SubscribeLocalEvent]
    private void OnCollect(CEStatisticsCollectEvent ev)
    {
        var counted = new HashSet<NetUserId>();
        var query = EntityQueryEnumerator<CERoundsFinishedAliveTrackerComponent>();
        while (query.MoveNext(out var uid, out var tracker))
        {
            if (_mobState.IsDead(uid))
                continue;

            if (!counted.Add(tracker.Player))
                continue;

            _statistic.AddStatistic(tracker.Player, CEStatistics.RoundsFinishedAlive);

            if (tracker.Job is { } job)
                _statistic.AddStatistic(tracker.Player, CEStatistics.RoundsFinishedAliveJob, job);
        }
    }
}
