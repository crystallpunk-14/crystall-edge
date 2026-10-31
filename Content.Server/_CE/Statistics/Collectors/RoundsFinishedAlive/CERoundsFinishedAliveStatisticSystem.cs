using Content.Shared._CE.Character;
using Content.Shared.Mind;
using Content.Shared.Roles.Jobs;
using Robust.Shared.Network;

namespace Content.Server._CE.Statistics.Collectors.RoundsFinishedAlive;

/// <summary>
/// Counts <see cref="CEStatistics.RoundsFinishedAlive"/> for characters whose body is still alive in the round at its end.
/// </summary>
public sealed partial class CERoundsFinishedAliveStatisticSystem : EntitySystem
{
    [Dependency] private CEStatisticSystem _statistic = default!;
    [Dependency] private CECharacterSystem _character = default!;
    [Dependency] private SharedJobSystem _job = default!;

    [SubscribeLocalEvent]
    private void OnCollect(CEStatisticsCollectEvent ev)
    {
        var counted = new HashSet<NetUserId>();
        var query = EntityQueryEnumerator<CECharacterComponent, MindComponent>();
        while (query.MoveNext(out var character, out var mind))
        {
            if (mind.OriginalOwnerUserId is not { } player ||
                _character.GetBodyState(character.Body) is not (CEBodyState.Alive or CEBodyState.Critical))
            {
                continue;
            }

            if (!counted.Add(player))
                continue;

            _statistic.AddStatistic(player, CEStatistics.RoundsFinishedAlive);

            if (character.Job is not { } job)
                continue;

            _statistic.AddStatistic(player, CEStatistics.RoundsFinishedAliveJob, job);

            _job.TryGetAllDepartments(job.Id, out var departments);
            foreach (var department in departments)
            {
                _statistic.AddStatistic(player, CEStatistics.RoundsFinishedAliveDepartment, department.ID);
            }
        }
    }
}
