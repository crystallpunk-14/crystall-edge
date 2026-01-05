/*
 * This file is sublicensed under MIT License
 * https://github.com/space-wizards/space-station-14/blob/master/LICENSE.TXT
 */

using Content.Shared._CE.ZLevels.Core.Components;
using Content.Shared.Actions;
using Content.Shared.Maps;
using Robust.Shared.Map;

namespace Content.Shared._CE.ZLevels.Core.EntitySystems;

public abstract partial class CESharedZLevelsSystem
{
    [Dependency] protected readonly ITileDefinitionManager TilDefMan = default!;
    private void InitView()
    {
        SubscribeLocalEvent<CEZLevelViewerComponent, MoveEvent>(OnViewerMove);
        SubscribeAllEvent<ChangeViewedZLayerEvent>(OnChangeSelectedZLayer);
        SubscribeLocalEvent<CEZLevelViewerComponent, CEToggleZLevelLookUpAction>(OnToggleLookUp);
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

    private void OnChangeSelectedZLayer(ChangeViewedZLayerEvent args)
    {
        if (args.Target is null) return;

        var ent = GetEntity(args.Target);

        if (!TryComp<CEZLevelViewerComponent>(ent, out var comp))
            return;

        Entity<CEZLevelViewerComponent> target = (ent.Value, comp);

        TrySetViewedZLevel(target, args.NewValue);
    }
    private void SetViewedZLevel(Entity<CEZLevelViewerComponent> ent, int value)
    {
        ent.Comp.ViewedZLevel = value;
        DirtyField(ent, ent.Comp, nameof(CEZLevelViewerComponent.ViewedZLevel));
    }
    public bool TrySetViewedZLevel(Entity<CEZLevelViewerComponent> ent, int value)
    {
        //todo: Add Validations if needed

        SetViewedZLevel(ent, value);
        return true;
    }
}

public enum CEZLayerAlertSeverity : short
{
    negative = -1,
    neutral = 0,
    positive = 1,
}
public sealed partial class CEToggleZLevelLookUpAction : InstantActionEvent
{
}
