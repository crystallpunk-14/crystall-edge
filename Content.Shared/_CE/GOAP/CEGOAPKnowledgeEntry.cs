using Robust.Shared.Map;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._CE.GOAP;

/// <summary>
/// A single record in the GOAP knowledge store: what the NPC believes it knows about an entity.
/// Updated by perceptor systems; consumed by sensors and selectors.
/// </summary>
[DataDefinition]
public partial struct CEGOAPKnowledgeEntry
{
    /// <summary>
    /// Last position the entity was perceived at.
    /// </summary>
    [DataField]
    public EntityCoordinates LastSeenCoords;

    /// <summary>
    /// Game time of the most recent perception.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan LastSeenTime;

    /// <summary>
    /// Game time when this entry should be purged if not refreshed.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan ExpiresAt;
}
