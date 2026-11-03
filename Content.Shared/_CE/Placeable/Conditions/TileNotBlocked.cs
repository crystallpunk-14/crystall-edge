using Content.Shared.Maps;
using Content.Shared.Physics;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Placeable.Conditions;

/// <summary>
/// No anchored entity on the tile collides with the mask. Mobs and loose items don't count.
/// </summary>
public sealed partial class TileNotBlocked : CEPlacementCondition
{
    [DataField]
    public CollisionGroup Mask = CollisionGroup.Impassable;

    protected override LocId DefaultPopup => "ce-placeable-blocked";

    public override bool Check(IEntityManager entManager, CEPlacementTarget target)
    {
        return !entManager.System<TurfSystem>().IsTileBlocked(target.Grid, target.Tile, Mask, target.Grid.Comp);
    }
}
