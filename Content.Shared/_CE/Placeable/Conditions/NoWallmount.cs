using Content.Shared._CE.Wallmount;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Placeable.Conditions;

/// <summary>
/// No other wallmount hangs in the tile.
/// </summary>
public sealed partial class NoWallmount : CEPlacementCondition
{
    protected override LocId DefaultPopup => "ce-placeable-occupied";

    public override bool Check(IEntityManager entManager, CEPlacementTarget target)
    {
        return !entManager.System<CEWallmountSystem>().HasWallmountAt(target.Grid, target.Tile);
    }
}
