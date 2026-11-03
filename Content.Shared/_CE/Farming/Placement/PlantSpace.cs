using Content.Shared._CE.Placeable;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Farming.Placement;

/// <summary>
/// No other plant or solid obstacle takes the tile.
/// </summary>
public sealed partial class PlantSpace : CEPlacementCondition
{
    protected override LocId DefaultPopup => "ce-farming-soil-occupied";

    public override bool Check(IEntityManager entManager, CEPlacementTarget target)
    {
        return !entManager.System<CESharedFarmingSystem>().IsTileOccupied(target.Grid, target.Tile);
    }
}
