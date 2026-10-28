using Content.Shared.Roles;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Server._CE.Statistics.Collectors.RoundsFinishedAlive;

/// <summary>
/// Marks a character spawned from the lobby, so its survival is counted at round end.
/// </summary>
[RegisterComponent, Access(typeof(CERoundsFinishedAliveStatisticSystem))]
public sealed partial class CERoundsFinishedAliveTrackerComponent : Component
{
    [DataField]
    public NetUserId Player;

    [DataField]
    public ProtoId<JobPrototype>? Job;
}
