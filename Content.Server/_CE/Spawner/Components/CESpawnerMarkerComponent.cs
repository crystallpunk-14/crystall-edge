using Content.Shared.Tag;
using Robust.Shared.Prototypes;

namespace Content.Server._CE.Spawner.Components;

/// <summary>
/// CEStationSpawnerDistributionComponent attempts to spawn entities on these markers at the beginning of the round.
/// </summary>
[RegisterComponent]
public sealed partial class CESpawnerMarkerComponent : Component
{
    /// <summary>
    /// The weight of entities from a particular group.
    /// </summary>
    [DataField(required: true)]
    public Dictionary<ProtoId<TagPrototype>, float> WeightedTypes = new();

    /// <summary>
    /// The number of entities spawned by this marker. The more entities are spawned, the lower the priority for subsequent spawn attempts.
    /// </summary>
    [DataField]
    public int SpawnerCounter = 0;
}
