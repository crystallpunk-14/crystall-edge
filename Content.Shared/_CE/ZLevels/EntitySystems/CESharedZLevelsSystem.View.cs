using Content.Shared.Actions;
using Content.Shared.Camera;
using Content.Shared.Coordinates;
using Content.Shared.Maps;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.ZLevels.EntitySystems;

public abstract partial class CESharedZLevelsSystem
{
    [Dependency] protected readonly ITileDefinitionManager TilDefMan = default!;
    [Dependency] protected readonly SharedEyeSystem SharedEyeSystem = default!;


    private string _prototype = "test";

    private void InitView()
    {
        SubscribeLocalEvent<CEZLevelViewerComponent, MoveEvent>(OnViewerMove);
        SubscribeLocalEvent<CEZLevelViewerComponent, CEToggleZLevelLookUpAction>(OnToggleLookUp);
        SubscribeLocalEvent<CEZLevelViewerComponent, ComponentInit>(OnOffset);
    }
    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);
    }
    protected virtual void OnOffset(Entity<CEZLevelViewerComponent> ent, ref ComponentInit args)
    {

        var query = EntityQueryEnumerator<EyeComponent, CEZLevelViewerComponent>();
        while (query.MoveNext(out var uid, out var eye, out var zLevel))
        {
            //    args.Scale = new MapCoordinates(eye.Eye.Position.Position, new MapId(zLevel.ViewedZLevel));
        }
    }


    protected virtual void OnViewerMove(Entity<CEZLevelViewerComponent> ent, ref MoveEvent args)
    {

        if (!ent.Comp.LookUp)
            return;

        if (!HasOpaqueAbove(ent))
            return;

        ent.Comp.LookUp = false;
        DirtyField(ent, ent.Comp, nameof(CEZLevelViewerComponent.LookUp));
    }

    private void OnToggleLookUp(Entity<CEZLevelViewerComponent> ent, ref CEToggleZLevelLookUpAction args)
    {
        var view = Spawn(_prototype, new MapCoordinates(ent.Owner.ToCoordinates().Position, new MapId(ent.Comp.ViewedZLevel)));
        SharedEyeSystem.SetTarget(ent, view);
        if (args.Handled)
            return;

        args.Handled = true;

        if (HasOpaqueAbove(ent))
        {
            _popup.PopupClient(Loc.GetString("ce-zlevel-look-up-fail"), ent, ent);
            return;
        }

        ent.Comp.LookUp = !ent.Comp.LookUp;
        DirtyField(ent, ent.Comp, nameof(CEZLevelViewerComponent.LookUp));
    }

    public bool HasOpaqueAbove(EntityUid ent, Entity<CEZLevelMapComponent?>? currentMapUid = null)
    {
        currentMapUid ??= Transform(ent).MapUid;

        if (currentMapUid is null)
            return false;

        if (!TryMapUp(currentMapUid.Value, out var mapAboveUid))
            return false;

        if (!_gridQuery.TryComp(mapAboveUid.Value, out var mapAboveGrid))
            return false;

        if (!_map.TryGetTileRef(mapAboveUid.Value, mapAboveGrid, _transform.GetWorldPosition(ent), out var tileRef))
            return false;

        var tileDef = (ContentTileDefinition)TilDefMan[tileRef.Tile.TypeId];

        return !tileDef.Transparent;
    }
}

public sealed partial class CEToggleZLevelLookUpAction : InstantActionEvent
{
}
