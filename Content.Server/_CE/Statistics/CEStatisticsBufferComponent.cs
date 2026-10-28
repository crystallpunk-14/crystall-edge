using Robust.Shared.Network;

namespace Content.Server._CE.Statistics;

/// <summary>
/// Buffer of player statistics gained this round, saved to the database at round end.
/// Lives on a single nullspace entity that is deleted on round restart.
/// </summary>
[RegisterComponent, Access(typeof(CEStatisticSystem))]
public sealed partial class CEStatisticsBufferComponent : Component
{
    [DataField]
    public Dictionary<NetUserId, Dictionary<string, long>> Values = new();

    /// <summary>
    /// Set once the round-end collection is done; later additions are ignored.
    /// </summary>
    [DataField]
    public bool Closed;
}
