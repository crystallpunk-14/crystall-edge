using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Server._CE.Statistics;

/// <summary>
/// All player statistics. Keys are stored in the database: renaming one drops collected data.
/// </summary>
public static class CEStatistics
{
    /// <summary>
    /// Rounds the player's lobby character survived until the round end.
    /// </summary>
    public static readonly CEStatisticDef RoundsFinishedAlive = new("rounds_finished_alive");

    /// <summary>
    /// <see cref="RoundsFinishedAlive"/> split by the job the character spawned with.
    /// </summary>
    public static readonly CEStatisticDef<ProtoId<JobPrototype>> RoundsFinishedAliveJob = new("rounds_finished_alive.job");

    /// <summary>
    /// <see cref="RoundsFinishedAlive"/> split by every department of the job the character spawned with.
    /// </summary>
    public static readonly CEStatisticDef<ProtoId<DepartmentPrototype>> RoundsFinishedAliveDepartment = new("rounds_finished_alive.department");
}
