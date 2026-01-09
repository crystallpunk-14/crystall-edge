/*
 * This file is sublicensed under MIT License
 * https://github.com/space-wizards/space-station-14/blob/master/LICENSE.TXT
 */

using System.Diagnostics.CodeAnalysis;
using Content.Shared._CE.ZLevels.Core.Components;
using Content.Shared.Actions;
using Content.Shared.Alert;
using Content.Shared.Alert.Components;
using Content.Shared.Maps;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.ZLevels.Core.EntitySystems;

public abstract partial class CESharedZLevelsSystem
{
    [Dependency] protected readonly ITileDefinitionManager TilDefMan = default!;
    [Dependency] private readonly AlertsSystem _alert = default!;

    private void InitView()
    {
        SubscribeLocalEvent<CEZLevelViewerComponent, MoveEvent>(OnViewerMove);
        SubscribeAllEvent<ChangeViewedZLayerEvent>(OnChangeSelectedZLayer);
        SubscribeLocalEvent<CEZLevelViewerComponent, CEViewedZLayerChangedEvent>(OnViewedZLayerChanged);

        SubscribeLocalEvent<CEZLevelViewerComponent, CEToggleZLevelLookUpAction>(OnToggleLookUp);
        SubscribeLocalEvent<CEZLevelViewerComponent, ComponentStartup>(OnZLevelViewerStartup);
        SubscribeLocalEvent<CEZLevelViewerComponent, GetGenericAlertCounterAmountEvent>(OnGetCounterAmount);
    }

    private void OnZLevelViewerStartup(Entity<CEZLevelViewerComponent> ent, ref ComponentStartup args)
    {
        UpdateAlert(ent);
    }

    private void OnGetCounterAmount(Entity<CEZLevelViewerComponent> ent, ref GetGenericAlertCounterAmountEvent args)
    {
        if (args.Handled)
            return;
        if (args.Alert != ent.Comp.ZLayerAlert)
            return;

        var selected_z_layer = ent.Comp.ViewedZLevel;


        args.Amount = Math.Abs(selected_z_layer);
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

    public bool HasOpaqueAbove(EntityUid ent, int range, [NotNullWhen(true)] out Entity<CEZLevelMapComponent>? ceiling, Entity<CEZLevelMapComponent?>? currentMapUid = null)
    {
        currentMapUid ??= Transform(ent).MapUid;
        ceiling = null;
        if (range < 0) throw new($"{nameof(range)} value of {range} is Negative.");

        if (currentMapUid is null)
            return false;

        if (!TryMapOffset(currentMapUid.Value, range, out var mapAboveUid))
            return false;

        if (!_gridQuery.TryComp(mapAboveUid.Value, out var mapAboveGrid))
            return false;

        if (!_map.TryGetTileRef(mapAboveUid.Value, mapAboveGrid, _transform.GetWorldPosition(ent), out var tileRef))
            return false;

        var tileDef = (ContentTileDefinition)TilDefMan[tileRef.Tile.TypeId];
        ceiling=mapAboveUid;
        return !tileDef.Transparent;

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

    private void OnViewedZLayerChanged(Entity<CEZLevelViewerComponent> ent, ref CEViewedZLayerChangedEvent args)
    {
        CEZLayerAlertSeverity newSeverity;
        if (ent.Comp.ViewedZLevel > 0) newSeverity = CEZLayerAlertSeverity.positive;
        else if (ent.Comp.ViewedZLevel < 0) newSeverity = CEZLayerAlertSeverity.negative;
        else newSeverity = CEZLayerAlertSeverity.neutral;

        if (newSeverity == ent.Comp.ZLayerAlertSeverity) return;

        ent.Comp.ZLayerAlertSeverity = newSeverity;
        UpdateAlert(ent);
    }
    private void UpdateAlert(Entity<CEZLevelViewerComponent> target)
    {
        _alert.ShowAlert(target.Owner, target.Comp.ZLayerAlert, (short)target.Comp.ZLayerAlertSeverity);
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
        RaiseLocalEvent(ent, new CEViewedZLayerChangedEvent(value));
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
