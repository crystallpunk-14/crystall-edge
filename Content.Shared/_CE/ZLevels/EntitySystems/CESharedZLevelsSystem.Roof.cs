namespace Content.Shared._CE.ZLevels.EntitySystems;

public abstract partial class CESharedZLevelsSystem
{
    private void InitRoof()
    {
        SubscribeLocalEvent<CEZLevelMapComponent, TileChangedEvent>(OnTileChanged);
    }

    private void OnTileChanged(Entity<CEZLevelMapComponent> ent, ref TileChangedEvent args)
    {
        if (!TryMapDown((ent.Owner, ent.Comp), out var belowMapUid))
            return;

        //Update rooving below map
        foreach (var change in args.Changes)
        {
            Roof.SetRoof(belowMapUid.Value.Owner, change.GridIndices, !change.NewTile.IsEmpty);
        }
    }
}
