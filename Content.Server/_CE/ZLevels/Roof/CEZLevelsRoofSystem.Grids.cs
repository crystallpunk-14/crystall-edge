/*
 * This file is sublicensed under MIT License
 * https://github.com/space-wizards/space-station-14/blob/master/LICENSE.TXT
 */

using System.Linq;
using System.Numerics;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Content.Shared._CE.ZLevels.Core.Components;
using Content.Shared._CE.ZLevels.Core.EntitySystems;
using Content.Shared._CE.ZLevels.Roof;
using Content.Shared.Light.Components;
using Content.Shared.Maps;

namespace Content.Server._CE.ZLevels.Roof;

public sealed partial class CEZLevelsRoofSystem
{
    [Dependency] private EntityQuery<CEZGridComponent> _zgridQuery = default!;
    [Dependency] private EntityQuery<CEZGridNetworkComponent> _zGridNetworkQuery = default!;

    [SubscribeLocalEvent]
    private void OnZGridNetworkUpdate(Entity<CEZGridNetworkComponent> ent, ref CEZLevelGridNetworkUpdatedEvent args)
    {
        RecalculateGridRoofs(ent);
    }

    [SubscribeLocalEvent]
    private void OnZGridMapInit(Entity<CEZGridComponent> ent, ref MapInitEvent args)
    {
        EnsureComp<CEZLevelRoofComponent>(ent.Owner);
    }

    public void RecalculateGridRoofs(Entity<CEZGridNetworkComponent> network)
    {
        _roofMap.Clear();

        var sorted = network.Comp.Grids
            .Select(g => (Grid: g, Depth: ZLevel.TryGetGridZDepth(g)))
            .Where(x => x.Depth.HasValue)
            .OrderByDescending(x => x.Depth!.Value);

        foreach (var (gridUid, _) in sorted)
        {
            RemCompDeferred<ImplicitRoofComponent>(gridUid); //hack but that way we dont need edit vanilla code

            if (!GridQuery.TryComp(gridUid, out var grid))
                continue;
            var roofComp = EnsureComp<RoofComponent>(gridUid);
            var enumerator = Map.GetAllTilesEnumerator(gridUid, grid);

            while (enumerator.MoveNext(out var tileRef))
            {
                var worldTile = ZLevel.GridTileToWorldTile(gridUid, grid, tileRef.Value.GridIndices);

                Roof.SetRoof((gridUid, grid, roofComp),
                    tileRef.Value.GridIndices,
                    _roofMap.Contains(worldTile));

                var tileDef = (ContentTileDefinition)TilDefMan[tileRef.Value.Tile.TypeId];
                if (!tileDef.Transparent)
                    _roofMap.Add(worldTile);
            }
        }
    }

    private void OnGridTileChanged(
        EntityUid gridUid,
        MapGridComponent currentMapGrid,
        RoofComponent currentRoof,
        TileChangedEvent args)
    {
        if (!ZLevel.TryGetGridNetwork(gridUid, out var network))
            return;

        if (ZLevel.TryGetGridZDepth(gridUid) is not { } ownDepth)
            return;

        // Topmost first, so `covered` below only ever re-latches to true by walking downward into
        // a level with its own opaque tile — never by "seeing" a level further above.
        var below = network.Comp.Grids
            .Select(g => (Grid: g, Depth: ZLevel.TryGetGridZDepth(g)))
            .Where(x => x.Depth is { } d && d < ownDepth)
            .OrderByDescending(x => x.Depth!.Value)
            .ToList();

        if (below.Count == 0)
            return;

        foreach (var change in args.Changes)
        {
            var worldTile = ZLevel.GridTileToWorldTile(gridUid, currentMapGrid, change.GridIndices);
            var tileDef = (ContentTileDefinition)TilDefMan[change.NewTile.TypeId];
            var roovedAbove = Roof.IsRooved((gridUid, currentMapGrid, currentRoof), change.GridIndices);
            var covered = roovedAbove || !tileDef.Transparent;

            foreach (var (otherGrid, _) in below)
            {
                if (!GridQuery.TryComp(otherGrid, out var otherMapGrid))
                    continue;

                if (!TryWorldTileToLocalTile(otherGrid, otherMapGrid, worldTile, out var localTile))
                    continue;

                if (!Map.TryGetTileRef(otherGrid, otherMapGrid, localTile, out var tileRef) || tileRef.Tile.IsEmpty)
                    continue; // nothing here on this level — neither marked nor able to re-shield below

                var otherRoof = EnsureComp<RoofComponent>(otherGrid);
                Roof.SetRoof((otherGrid, otherMapGrid, otherRoof), localTile, covered);

                var otherTileDef = (ContentTileDefinition)TilDefMan[tileRef.Tile.TypeId];
                if (!otherTileDef.Transparent)
                    covered = true; // this level's own solid tile re-shields everything further down
            }
        }
    }

    /// <summary>
    /// Resolves a world tile (shared X,Y convention across Z-level maps — see
    /// <see cref="CESharedZLevelsSystem"/>'s <c>TryMove</c>) into <paramref name="grid"/>'s own local
    /// tile index. Z-levels are separate MapIds, so this deliberately tags the shared (X,Y) with
    /// <paramref name="gridUid"/>'s own MapId rather than the caller's, exploiting that convention.
    /// Non-throwing: <paramref name="gridUid"/> could have been deleted between being read out of the
    /// z-grid network and this call (e.g. a ZCollapse that ate the whole grid).
    /// </summary>
    private bool TryWorldTileToLocalTile(EntityUid gridUid, MapGridComponent grid, Vector2i worldTile, out Vector2i localTile)
    {
        localTile = default;
        if (!XformQuery.TryGetComponent(gridUid, out var xform))
            return false;

        var worldPos = new Vector2(worldTile.X + 0.5f, worldTile.Y + 0.5f);
        localTile = Map.TileIndicesFor(gridUid, grid, new MapCoordinates(worldPos, xform.MapID));
        return true;
    }
}
