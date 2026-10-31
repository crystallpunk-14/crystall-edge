using Robust.Shared.Network;

namespace Content.Server._CE.Statistics;

/// <summary>
/// Raised broadcast at round end right before the buffer is saved to the database.
/// Subscribe to add statistics that are evaluated once per round.
/// </summary>
public sealed class CEStatisticsCollectEvent : EntityEventArgs;

/// <summary>
/// Raised broadcast per player after the round's statistics were saved to the database.
/// </summary>
public sealed class CEStatisticUpdatedEvent(
    NetUserId userId,
    IReadOnlyDictionary<string, long> roundValues,
    IReadOnlyDictionary<string, long> totalValues) : EntityEventArgs
{
    public readonly NetUserId UserId = userId;

    /// <summary>
    /// Values the player gained this round.
    /// </summary>
    public readonly IReadOnlyDictionary<string, long> RoundValues = roundValues;

    /// <summary>
    /// All-time values of the keys touched this round, including this round.
    /// </summary>
    public readonly IReadOnlyDictionary<string, long> TotalValues = totalValues;

    public long GetRoundValue(CEStatisticDef stat) => RoundValues.GetValueOrDefault(stat.Key);

    public long GetRoundValue<T>(CEStatisticDef<T> stat, T arg) where T : notnull
    {
        return RoundValues.GetValueOrDefault(stat.GetKey(arg));
    }

    /// <summary>
    /// All-time value of the stat, or 0 when it wasn't touched this round.
    /// </summary>
    public long GetTotalValue(CEStatisticDef stat) => TotalValues.GetValueOrDefault(stat.Key);

    public long GetTotalValue<T>(CEStatisticDef<T> stat, T arg) where T : notnull
    {
        return TotalValues.GetValueOrDefault(stat.GetKey(arg));
    }
}
