using Content.Shared.EntityTable.EntitySelectors;
using Content.Shared.Tag;
using Robust.Shared.Prototypes;

namespace Content.Server._CE.Spawner.Components;

/// <summary>
/// Guaranteed to distribute selected loot across the station using special markers as target points
/// </summary>
[RegisterComponent]
public sealed partial class CEStationSpawnerDistributionComponent : Component
{
    /// <summary>
    /// Guaranteed spawn of these entities on the specified spawn types
    /// </summary>
    [DataField(required: true)]
    public Dictionary<ProtoId<TagPrototype>, EntityTableSelector> Spawns = new();
}
