using Content.Server._CE.Demiplane.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server._CE.Demiplane.Components;

/// <summary>
/// Marks a station mid-teleport to a new demiplane stage: the old stage is already cleared out, a
/// new one may be generating in the background, and nothing gets spliced into the z-network until
/// both <see cref="ReadyMaps"/> is set and <see cref="EndTime"/> has passed. Removed once the splice
/// completes. Added and consumed by <see cref="CEDemiplaneSystem"/>.
/// </summary>
[RegisterComponent, AutoGenerateComponentPause, Access(typeof(CEDemiplaneSystem))]
public sealed partial class CEStationDemiplaneTeleportationComponent : Component
{
    /// <summary>
    /// When the dramatic pause ends. Splicing never happens before this, even if generation
    /// finished earlier — the wait itself is a gameplay beat, not just a loading screen.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan EndTime;

    /// <summary>
    /// What we're teleporting to. Null means "back to the void" — nothing gets spliced in.
    /// </summary>
    [ViewVariables]
    public ProtoId<CEDemiplaneLocationPrototype>? Location;

    /// <summary>
    /// Freshly generated maps waiting to be spliced in, nearest-to-island first.
    /// Null while generation is still running; an empty list once there is nothing to add
    /// (a null <see cref="Location"/>, resolved immediately without a generation job).
    /// </summary>
    [ViewVariables]
    public List<EntityUid>? ReadyMaps;
}
