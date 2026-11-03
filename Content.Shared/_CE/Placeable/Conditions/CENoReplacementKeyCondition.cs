namespace Content.Shared._CE.Placeable.Conditions;

/// <summary>
/// No anchored entity on the tile has a <see cref="PlacementReplacementComponent"/> with this key.
/// Tells apart entity families like big and medium pipes, which may share a tile with each other.
/// </summary>
public sealed partial class CENoReplacementKeyCondition : CEPlacementCondition
{
    [DataField(required: true)]
    public string Key = string.Empty;

    public override bool Check(IEntityManager entManager, CEPlacementTarget target)
    {
        var map = entManager.System<SharedMapSystem>();

        var enumerator = map.GetAnchoredEntitiesEnumerator(target.Grid, target.Grid.Comp, target.Tile);
        while (enumerator.MoveNext(out var uid))
        {
            if (entManager.TryGetComponent<PlacementReplacementComponent>(uid.Value, out var replacement)
                && replacement.Key == Key)
                return false;
        }

        return true;
    }
}
