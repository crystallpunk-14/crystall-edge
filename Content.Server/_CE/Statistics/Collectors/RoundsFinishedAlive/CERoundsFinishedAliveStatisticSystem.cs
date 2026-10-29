using Content.Server._CE.GameTicking.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Roles.Jobs;
using Robust.Shared.Network;

namespace Content.Server._CE.Statistics.Collectors.RoundsFinishedAlive;

/// <summary>
/// Counts <see cref="CEStatistics.RoundsFinishedAlive"/> for main characters that are not dead at round end.
/// </summary>
public sealed partial class CERoundsFinishedAliveStatisticSystem : EntitySystem
{
    [Dependency] private CEStatisticSystem _statistic = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private SharedJobSystem _job = default!;

    [SubscribeLocalEvent]
    private void OnCollect(CEStatisticsCollectEvent ev)
    {
        var counted = new HashSet<NetUserId>();
        var query = EntityQueryEnumerator<CEMainCharacterComponent>();
        while (query.MoveNext(out var uid, out var character))
        {
            if (_mobState.IsDead(uid))
                continue;

            if (!counted.Add(character.Player))
                continue;

            _statistic.AddStatistic(character.Player, CEStatistics.RoundsFinishedAlive);

            if (character.Job is not { } job)
                continue;

            _statistic.AddStatistic(character.Player, CEStatistics.RoundsFinishedAliveJob, job);

            _job.TryGetAllDepartments(job.Id, out var departments);
            foreach (var department in departments)
            {
                _statistic.AddStatistic(character.Player, CEStatistics.RoundsFinishedAliveDepartment, department.ID);
            }
        }
    }
}
