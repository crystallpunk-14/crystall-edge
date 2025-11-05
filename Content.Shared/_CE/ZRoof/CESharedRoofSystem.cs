using Content.Shared._CE.ZLevels;
using Content.Shared._CE.ZLevels.EntitySystems;
using Content.Shared.Light.EntitySystems;
using Robust.Shared.Map.Components;

namespace Content.Shared._CE.ZRoof;

/// <summary>
/// Systems that automatically covers tiles with roofs (or removes roofs)
/// if there is a tile on one of the levels above in the ZLevels network.
/// </summary>
public abstract class CESharedRoofSystem : EntitySystem
{
    [Dependency] protected readonly CESharedZLevelsSystem ZLevel = default!;
    [Dependency] protected readonly SharedRoofSystem Roof = default!;

    protected EntityQuery<MapGridComponent> GridQuery;

    public override void Initialize()
    {
        base.Initialize();

        GridQuery = GetEntityQuery<MapGridComponent>();

        SubscribeLocalEvent<CEZLevelMapComponent, TileChangedEvent>(OnTileChanged);
    }

    private void OnTileChanged(Entity<CEZLevelMapComponent> ent, ref TileChangedEvent args)
    {
        if (!ZLevel.TryMapDown((ent.Owner, ent.Comp), out var belowMapUid))
            return;

        //Update rooving below map
        foreach (var change in args.Changes)
        {
            Roof.SetRoof(belowMapUid.Value.Owner, change.GridIndices, !change.NewTile.IsEmpty);
        }
    }
}
