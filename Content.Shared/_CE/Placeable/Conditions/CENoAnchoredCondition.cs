using Content.Shared.Whitelist;

namespace Content.Shared._CE.Placeable.Conditions;

/// <summary>
/// No anchored entity on the tile matches the whitelist. Keeps duplicates like two conveyors out of one tile.
/// </summary>
public sealed partial class CENoAnchoredCondition : CEPlacementCondition
{
    [DataField(required: true)]
    public EntityWhitelist Whitelist = new();

    public override bool Check(IEntityManager entManager, CEPlacementTarget target)
    {
        var map = entManager.System<SharedMapSystem>();
        var whitelist = entManager.System<EntityWhitelistSystem>();

        var enumerator = map.GetAnchoredEntitiesEnumerator(target.Grid, target.Grid.Comp, target.Tile);
        while (enumerator.MoveNext(out var uid))
        {
            if (whitelist.IsWhitelistPass(Whitelist, uid.Value))
                return false;
        }

        return true;
    }
}
