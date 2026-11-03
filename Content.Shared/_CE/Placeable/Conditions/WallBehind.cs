using Content.Shared._CE.Wallmount;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Placeable.Conditions;

/// <summary>
/// There is a wall or a window right behind the entity, opposite to the direction it faces.
/// </summary>
public sealed partial class WallBehind : CEPlacementCondition
{
    protected override LocId DefaultPopup => "ce-placeable-no-wall";

    public override bool Check(IEntityManager entManager, CEPlacementTarget target)
    {
        var map = entManager.System<SharedMapSystem>();
        var wallmount = entManager.System<CEWallmountSystem>();

        var wallTile = target.Tile - target.Direction.ToIntVec();
        var enumerator = map.GetAnchoredEntitiesEnumerator(target.Grid, target.Grid.Comp, wallTile);
        while (enumerator.MoveNext(out var uid))
        {
            if (wallmount.IsWallSurface(uid.Value))
                return true;
        }

        return false;
    }
}
