using Content.Shared.Maps;
using Robust.Shared.Prototypes;

namespace Content.Server._CE.AutoTilePlacement;

/// <summary>
/// Automatically place tile under spawned entity, if this entity was spawned from PlacementManager
/// </summary>
[RegisterComponent, Access(typeof(CEAutoTilePlacementComponent))]
public sealed partial class CEAutoTilePlacementComponent : Component
{
    [DataField]
    public ProtoId<ContentTileDefinition> Tile = "CEStone";
}
