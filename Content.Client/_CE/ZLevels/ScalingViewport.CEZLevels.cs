using System.Numerics;
using Content.Client._CE.ZLevels;
using Robust.Client.Graphics;
using Robust.Shared.Graphics;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;

namespace Content.Client.Viewport;

public sealed partial class ScalingViewport
{
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IEyeManager _eyeManager = default!;

    private CEClientZLevelsSystem? _zLevels;

    private EntityQuery<TransformComponent>? _xformQuery;

    private IEye? _fallbackEye;

    /// <summary>
    /// From the incoming list of maps, we filter only those that require rendering.
    /// </summary>
    public List<EntityUid> GetFilteredMapList(List<EntityUid> sourceList, EntityUid currentMap)
    {
        var mapList = new List<EntityUid>();

        if (_eye is null)
            return mapList;

        var mapIdx = sourceList.IndexOf(currentMap);
        if (mapIdx < 0)
            return mapList;

        for (var i = mapIdx; i >= 0; i--)
        {
            var targetMap = sourceList[i];
            mapList.Add(targetMap);

            //if (!TryFindEmptyTiles(targetMap))
            //    break;
        }

        // Reverse a new list
        if (mapList.Count > 0)
        {
            var tempList = new List<EntityUid>(mapList.Count);
            for (var i = mapList.Count - 1; i >= 0; i--)
            {
                tempList.Add(mapList[i]);
            }

            mapList = tempList;
        }

        return mapList;
    }

    /// <summary>
    /// We are looking for at least one empty tile on the screen.
    /// This is used to ensure that it makes sense to draw the z-planes and that they are visible.
    /// </summary>
    public bool TryFindEmptyTiles(EntityUid mapUid)
    {
        if (_xformQuery is null || !_xformQuery.Value.TryComp(mapUid, out var xform))
            return true;

        var drawBox = GetDrawBox();

        var bottomLeftPos = _eyeManager.ScreenToMap(drawBox.BottomLeft).Position;
        var topRightPos = _eyeManager.ScreenToMap(drawBox.TopRight).Position;
        var mapId = xform.MapID;

        var mapCoordsBottomLeft = new MapCoordinates(bottomLeftPos, mapId);
        var mapCoordsTopRight = new MapCoordinates(topRightPos, mapId);

        if (!_mapManager.TryFindGridAt(mapUid, mapCoordsBottomLeft.Position, out _, out var grid))
            return true;

        var tileBottomLeft = grid.TileIndicesFor(mapCoordsBottomLeft);
        var tileTopRight = grid.TileIndicesFor(mapCoordsTopRight);

        var minX = tileBottomLeft.X - 1;
        var maxX = tileTopRight.X + 1;
        var minY = tileBottomLeft.Y - 1;
        var maxY = tileTopRight.Y + 1;

        Vector2i tilePos = default;

        for (tilePos.X = minX; tilePos.X <= maxX; tilePos.X++)
        {
            for (tilePos.Y = minY; tilePos.Y <= maxY; tilePos.Y++)
            {
                var tile = grid.GetTileRef(tilePos);

                if (tile.Tile.IsEmpty)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private bool TryRenderZLevels(IRenderHandle handle, IClydeViewport viewport)
    {
        if (_eye is null)
            return false;

        _fallbackEye = _eye;

        // Cache frequently accessed components/systems
        _xformQuery ??= _entityManager.GetEntityQuery<TransformComponent>();

        var drawBox = GetDrawBox();

        // Cache systems and components
        _zLevels ??= _entityManager.System<CEClientZLevelsSystem>();

        var mapId = _eye.Position.MapId;
        var mapEntityId = _mapManager.GetMapEntityIdOrThrow(mapId);

        var drawMaps = _zLevels.GetAllMapsBelow(mapEntityId);
        if (drawMaps.Count == 0)
            return false;

        for (var i = 0; i < drawMaps.Count; i++)
        {
            var toDraw = drawMaps[i];
            var mapComp = _entityManager.GetComponent<MapComponent>(toDraw);

            var depth = drawMaps.Count - i; // reversed depth index

            var pos = new MapCoordinates(_eye.Position.Position, mapComp.MapId);

            var zEye = new ZEye
            {
                Position = pos,
                DrawFov = false,
                DrawLight = false,
                Offset = _eye.Offset + new Vector2(0f, depth * 0.8f),
                Rotation = _eye.Rotation,
                Scale = _eye.Scale,
                Depth = depth,
            };

            viewport.Eye = zEye;
            viewport.ClearColor = i == 0 ? Color.Black : null;
            viewport.Render();
        }

        // Restore the Eye
        Eye = _fallbackEye;
        viewport.ClearColor = null;
        viewport.Eye = Eye;

        return true;
    }

    //FIXME: This is nasty!
    public sealed class ZEye : Robust.Shared.Graphics.Eye
    {
        public int Depth;
    }
}
