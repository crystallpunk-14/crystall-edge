using System.Numerics;
using Content.Server.Procedural;
using Content.Shared._CE.Procedural;
using Content.Shared._CE.ZLevels.Core.Components;
using Content.Shared.Decals;
using Content.Shared.Maps;
using Robust.Shared.EntitySerialization;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server._CE.Procedural;

public sealed partial class CEDungeonSystem
{
    private readonly List<CEDungeonRoom3DPrototype> _availableRooms = new();

    /// <summary>
    /// Gets a random dungeon room matching the specified size range.
    /// </summary>
    public CEDungeonRoom3DPrototype? GetRoomPrototype(Random random,
        Vector2i? minSize = null,
        Vector2i? maxSize = null,
        int? minHeight = null,
        int? maxHeight = null)
    {
        _availableRooms.Clear();

        foreach (var proto in _proto.EnumeratePrototypes<CEDungeonRoom3DPrototype>())
        {
            if (minSize is not null && (proto.Size.X < minSize.Value.X || proto.Size.Y < minSize.Value.Y))
                continue;

            if (maxSize is not null && (proto.Size.X > maxSize.Value.X || proto.Size.Y > maxSize.Value.Y))
                continue;

            if (minHeight is not null && proto.Height < minHeight.Value)
                continue;

            if (maxHeight is not null && proto.Height > maxHeight.Value)
                continue;

            _availableRooms.Add(proto);
        }

        if (_availableRooms.Count == 0)
            return null;

        // Weighted random selection.
        var totalWeight = 0f;
        foreach (var r in _availableRooms)
        {
            totalWeight += r.Weight;
        }

        var roll = (float)(random.NextDouble() * totalWeight);
        foreach (var r in _availableRooms)
        {
            roll -= r.Weight;
            if (roll <= 0f)
                return r;
        }

        return _availableRooms[^1];
    }

    public bool TrySpawn3DRoom(
        EntityUid gridUid,
        MapGridComponent grid,
        Matrix3x2 roomTransform,
        CEDungeonRoom3DPrototype room,
        HashSet<Vector2i>? reservedTiles = null,
        bool clearExisting = false)
    {
        if (!_proto.Resolve(room.ZLevelMap, out var indexedZMap))
            return false;
        // Try to get z-level information for the provided grid. If none exists we'll just
        // spawn everything onto the provided grid.
        if (!TryComp<CEZMapComponent>(gridUid, out var zMapComp))
            return false;

        for (var offset = 0; offset < room.Height; offset++)
        {
            var mapPath = indexedZMap.Maps[offset];
            var roomMap = GetOrCreateTemplate(mapPath);
            var templateMapUid = _maps.GetMapOrInvalid(roomMap);
            var templateGrid = Comp<MapGridComponent>(templateMapUid);
            var roomDimensions = room.Size;

            var finalRoomRotation = roomTransform.Rotation();

            var roomCenter = (room.Offset + room.Size / 2f) * grid.TileSize;
            var tileOffset = -roomCenter + grid.TileSizeHalfVector;
            _tiles.Clear();

            //Calculate target map
            var targetMapUid = gridUid;
            var targetGrid = grid;

            if (offset != 0)
            {
                if (!_zLevels.TryMapOffset((gridUid, zMapComp), offset, out var found))
                {
                    Log.Error($"Failed to find target map for dungeon room z-level offset {offset} on map {Transform(gridUid).MapID}");
                    continue;
                }

                targetMapUid = found;
                targetGrid = Comp<MapGridComponent>(targetMapUid);
            }

            // Load tiles for this layer
            for (var x = 0; x < roomDimensions.X; x++)
            {
                for (var y = 0; y < roomDimensions.Y; y++)
                {
                    var indices = new Vector2i(x + room.Offset.X, y + room.Offset.Y);
                    var tileRef = _maps.GetTileRef(templateMapUid, templateGrid, indices);

                    var tilePos = Vector2.Transform(indices + tileOffset, roomTransform);
                    var rounded = tilePos.Floored();

                    if (!clearExisting && reservedTiles?.Contains(rounded) == true)
                        continue;

                    if (room.IgnoreTile is not null)
                    {
                        if (_maps.TryGetTileDef(templateGrid, indices, out var tileDef) && room.IgnoreTile == tileDef.ID)
                            continue;
                    }

                    _tiles.Add((rounded, tileRef.Tile));

                    if (clearExisting)
                    {
                        var anchored = _maps.GetAnchoredEntities((targetMapUid, targetGrid), rounded);
                        foreach (var ent in anchored)
                        {
                            QueueDel(ent);
                        }
                    }
                }
            }

            var bounds = new Box2(room.Offset, room.Offset + room.Size);

            _maps.SetTiles(targetMapUid, targetGrid, _tiles);

            // Load entities from template into target map
            foreach (var templateEnt in _lookup.GetEntitiesIntersecting(templateMapUid, bounds, LookupFlags.Uncontained))
            {
                var templateXform = _xformQuery.GetComponent(templateEnt);
                var childPos = Vector2.Transform(templateXform.LocalPosition - roomCenter, roomTransform);

                if (!clearExisting && reservedTiles?.Contains(childPos.Floored()) == true)
                    continue;

                var childRot = templateXform.LocalRotation + finalRoomRotation;
                var protoId = _metaQuery.GetComponent(templateEnt).EntityPrototype?.ID;

                var ent = Spawn(protoId, new EntityCoordinates(targetMapUid, childPos));

                var childXform = _xformQuery.GetComponent(ent);
                var anchored = templateXform.Anchored;
                _transform.SetLocalRotation(ent, childRot, childXform);

                if (anchored && !childXform.Anchored)
                    _transform.AnchorEntity((ent, childXform), (targetMapUid, targetGrid));
                else if (!anchored && childXform.Anchored)
                    _transform.Unanchor(ent, childXform);
            }

            // Load decals
            if (TryComp<DecalGridComponent>(templateMapUid, out var loadedDecals))
            {
                EnsureComp<DecalGridComponent>(targetMapUid);

                foreach (var (_, decal) in _decals.GetDecalsIntersecting(templateMapUid, bounds, loadedDecals))
                {
                    var position = Vector2.Transform(decal.Coordinates + targetGrid.TileSizeHalfVector - roomCenter, roomTransform);
                    position -= targetGrid.TileSizeHalfVector;

                    if (!clearExisting && reservedTiles?.Contains(position.Floored()) == true)
                        continue;

                    var angle = (decal.Angle + finalRoomRotation).Reduced();

                    if (angle.Equals(Math.PI))
                    {
                        position += new Vector2(-1f / 32f, 1f / 32f);
                    }
                    else if (angle.Equals(-Math.PI / 2f))
                    {
                        position += new Vector2(-1f / 32f, 0f);
                    }
                    else if (angle.Equals(Math.PI / 2f))
                    {
                        position += new Vector2(0f, 1f / 32f);
                    }
                    else if (angle.Equals(Math.PI * 1.5f))
                    {
                        position += new Vector2(-1f / 32f, 0f);
                    }

                    var tilePos = position.Floored();

                    if (!_maps.TryGetTileRef(targetMapUid, targetGrid, tilePos, out var tileRef) || tileRef.Tile.IsEmpty)
                    {
                        _maps.SetTile(targetMapUid, targetGrid, tilePos, _tile.GetVariantTile((ContentTileDefinition)_tileDefManager[FallbackTileId], _random.Next()));
                    }

                    var result = _decals.TryAddDecal(
                        decal.Id,
                        new EntityCoordinates(targetMapUid, position),
                        out _,
                        decal.Color,
                        angle,
                        decal.ZIndex,
                        decal.Cleanable);

                    DebugTools.Assert(result);
                }
            }
        }

        return true;
    }

    private MapId GetOrCreateTemplate(ResPath atlasPath)
    {
        var query = AllEntityQuery<DungeonAtlasTemplateComponent>();
        DungeonAtlasTemplateComponent? comp;

        while (query.MoveNext(out var uid, out comp))
        {
            // Exists
            if (comp.Path.Equals(atlasPath))
                return Transform(uid).MapID;
        }

        var opts = new MapLoadOptions
        {
            DeserializationOptions = DeserializationOptions.Default with {PauseMaps = true},
            ExpectedCategory = FileCategory.Map
        };

        if (!_loader.TryLoadGeneric(atlasPath, out var res, opts) || !res.Maps.TryFirstOrNull(out var map))
            throw new Exception("Failed to load dungeon template.");

        comp = AddComp<DungeonAtlasTemplateComponent>(map.Value.Owner);
        comp.Path = atlasPath;
        return map.Value.Comp.MapId;
    }
}
