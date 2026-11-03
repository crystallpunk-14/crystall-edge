using Content.Shared._CE.Wallmount;

namespace Content.Shared._CE.Placeable.Conditions;

/// <summary>
/// No other wallmount hangs in the tile.
/// </summary>
public sealed partial class CENoWallmountCondition : CEPlacementCondition
{
    public override bool Check(IEntityManager entManager, CEPlacementTarget target)
    {
        return !entManager.System<CEWallmountSystem>().HasWallmountAt(target.Grid, target.Tile);
    }
}
