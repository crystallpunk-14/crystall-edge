using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Placeable.Conditions;

/// <summary>
/// No anchored entity on the tile matches the whitelist.
/// </summary>
public sealed partial class NoAnchored : CEPlacementCondition
{
    [DataField(required: true)]
    public EntityWhitelist Whitelist = new();

    protected override LocId DefaultPopup => "ce-placeable-occupied";

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
