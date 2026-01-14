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
    public const int MaxZLevelsBelowRendering = 3;
    public const int MaxZLevelsAboveRendering = 3;

    [Dependency] protected readonly ITileDefinitionManager TilDefMan = default!;
    private void InitView()
    {
        SubscribeLocalEvent<CEZLevelViewerComponent, CEToggleZLevelLookUpAction>(OnToggleLookUp);
    }

    private void OnToggleLookUp(Entity<CEZLevelViewerComponent> ent, ref CEToggleZLevelLookUpAction args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        ent.Comp.LookUp = !ent.Comp.LookUp;
        DirtyField(ent, ent.Comp, nameof(CEZLevelViewerComponent.LookUp));
    }

    /// <summary>
    /// Calculates the maximum number of z-levels above that are visible from the entity's current position.
    /// Stops when an opaque tile is encountered.
    /// </summary>
    public int GetVisibleZLevelsAbove(EntityUid ent, Entity<CEZLevelMapComponent?>? currentMapUid = null)
    {
        currentMapUid ??= Transform(ent).MapUid;

        if (currentMapUid is null)
            return 0;

        var visibleLevels = 0;
        var checkMapUid = currentMapUid.Value.Owner;

        for (var i = 1; i <= MaxZLevelsAboveRendering; i++)
        {
            if (!TryMapUp(checkMapUid, out var mapAboveUid))
                break;

            checkMapUid = mapAboveUid.Value.Owner;

            if (!_gridQuery.TryComp(mapAboveUid.Value, out var mapAboveGrid))
                break;

            if (!_map.TryGetTileRef(mapAboveUid.Value, mapAboveGrid, _transform.GetWorldPosition(ent), out var tileRef))
                break;

            var tileDef = (ContentTileDefinition)TilDefMan[tileRef.Tile.TypeId];

            if (!tileDef.Transparent)
                break;

            visibleLevels++;
        }

        return visibleLevels;
    }
}

public sealed partial class CEToggleZLevelLookUpAction : InstantActionEvent
{
}
