using Content.Shared._CE.ZLevels.EntitySystems;
using Content.Shared.Maps;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.ZLevels;

/// <summary>
/// Guarantees that there is at least one tile above the tile. If there is no tile, the specified one will be placed there.
/// </summary>
[RegisterComponent, Access(typeof(CESharedZLevelsSystem))]
public sealed partial class CEZLevelRoofPlacerComponent : Component
{
    [DataField(required: true)]
    public ProtoId<ContentTileDefinition> Tile;
}
