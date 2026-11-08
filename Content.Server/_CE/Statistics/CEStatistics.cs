using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
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

    /// <summary>
    /// Dark feasts the player finished as a werewolf.
    /// </summary>
    public static readonly CEStatisticDef DarkFeasts = new("dark_feasts");

    private static readonly Dictionary<string, CEStatisticDef> ByName = typeof(CEStatistics)
        .GetFields(BindingFlags.Public | BindingFlags.Static)
        .Where(f => f.FieldType == typeof(CEStatisticDef))
        .ToDictionary(f => f.Name, f => (CEStatisticDef) f.GetValue(null)!);

    /// <summary>
    /// Finds a plain statistic by its field name, for referencing statistics from YAML.
    /// </summary>
    public static bool TryGetByName(string name, [NotNullWhen(true)] out CEStatisticDef? stat)
    {
        return ByName.TryGetValue(name, out stat);
    }
}
