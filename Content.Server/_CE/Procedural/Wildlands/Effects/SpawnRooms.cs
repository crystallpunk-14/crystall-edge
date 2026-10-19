using System.Numerics;
using Content.Server._CE.Procedural.Generation;
using Content.Server._CE.Procedural.Generation.Masks;
using Content.Shared._CE.Procedural;
using Content.Shared.Destructible.Thresholds;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._CE.Procedural.Wildlands.Effects;

/// <summary>
/// Stamps rooms from <see cref="Rooms"/> (one picked at random per placement) at random spots, each with a random
/// base depth (where all its floors exist) and a random cardinal rotation. A spot is valid when the rotated footprint on the base level is all land, inside the
/// region, passes <see cref="Mask"/> and stays clear of earlier rooms by <see cref="Spacing"/>. Whatever was anchored
/// under the room is deleted on every floor. Running out of valid spots just places fewer rooms.
/// </summary>
public sealed partial class SpawnRooms : ICEWildlandsModifierEffect
{
    /// <summary>
    /// Pool of rooms; every placement picks one of them at random.
    /// </summary>
    [DataField(required: true)]
    public List<ProtoId<CEDungeonRoom3DPrototype>> Rooms = new();

    [DataField]
    public MinMax Count = new(1, 1);

    /// <summary>
    /// Minimum gap in tiles between two placed rooms.
    /// </summary>
    [DataField]
    public int Spacing;

    [DataField]
    public bool RandomRotation = true;

    /// <summary>
    /// Optional limits on the depth the room's bottom floor may land on.
    /// </summary>
    [DataField]
    public int? MinDepth;

    [DataField]
    public int? MaxDepth;

    /// <summary>
    /// Checked on the base level against every footprint tile.
    /// </summary>
    [DataField]
    public List<ICETileMask> Mask = new();

    public void Apply(CEProceduralGenerationContext context, IReadOnlyDictionary<int, EntityUid> levels)
    {
        var entMan = context.EntityManager;
        var dungeon = entMan.System<CEDungeonSystem>();
        if (Rooms.Count == 0)
            return;

        var rooms = new List<CEDungeonRoom3DPrototype>(Rooms.Count);
        foreach (var roomId in Rooms)
        {
            rooms.Add(context.Prototype.Index(roomId));
        }

        IRobustRandom random = new RobustRandom();
        random.SetSeed(context.Seed);

        var candidates = new List<(int Depth, Vector2i Tile)>();
        foreach (var (depth, map) in levels)
        {
            var grid = entMan.GetComponent<MapGridComponent>(map);
            var tiles = context.Map.GetAllTilesEnumerator(map, grid);
            while (tiles.MoveNext(out var tileRef))
            {
                if (context.Region.Contains(tileRef.Value.GridIndices))
                    candidates.Add((depth, tileRef.Value.GridIndices));
            }
        }

        // Candidates come out in grid order; shuffling keeps the picks seed-dependent but order-independent.
        random.Shuffle(candidates);

        var count = Count.Next(random);
        var placed = 0;
        var reserved = new HashSet<Vector2i>();
        var footprint = new List<Vector2i>();

        foreach (var (depth, anchor) in candidates)
        {
            if (placed >= count)
                break;

            var room = random.Pick(rooms);
            if (!FitsAt(depth, room.Height, levels))
                continue;

            var map = levels[depth];
            var grid = entMan.GetComponent<MapGridComponent>(map);
            var rotation = RandomRotation ? random.Next(4) : 0;

            GetFootprint(room.Size, anchor, rotation, footprint);
            if (!IsValid(context, map, grid, footprint, reserved))
                continue;

            if (!dungeon.TrySpawn3DRoom(map, grid, GetTransform(room.Size, anchor, rotation), room, clearExisting: true))
                continue;

            foreach (var tile in footprint)
            {
                for (var x = -Spacing; x <= Spacing; x++)
                {
                    for (var y = -Spacing; y <= Spacing; y++)
                    {
                        reserved.Add(tile + new Vector2i(x, y));
                    }
                }
            }

            placed++;
        }
    }

    private bool FitsAt(int depth, int height, IReadOnlyDictionary<int, EntityUid> levels)
    {
        if (MinDepth is { } min && depth < min || MaxDepth is { } max && depth > max)
            return false;

        for (var floor = 0; floor < height; floor++)
        {
            if (!levels.ContainsKey(depth + floor))
                return false;
        }

        return true;
    }

    private bool IsValid(
        CEProceduralGenerationContext context,
        EntityUid map,
        MapGridComponent grid,
        List<Vector2i> footprint,
        HashSet<Vector2i> reserved)
    {
        foreach (var tile in footprint)
        {
            if (reserved.Contains(tile) || !context.Region.Contains(tile))
                return false;

            var current = context.Map.GetTileRef(map, grid, tile).Tile;
            if (current.IsEmpty || !ICETileMask.PassesAll(context, map, grid, tile, current, Mask))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Target tiles of the room rotated by <paramref name="rotation"/> quarter turns, with its origin tile on
    /// <paramref name="anchor"/>. Integer math, so it matches <see cref="GetTransform"/> exactly.
    /// </summary>
    private static void GetFootprint(Vector2i size, Vector2i anchor, int rotation, List<Vector2i> footprint)
    {
        footprint.Clear();
        for (var x = 0; x < size.X; x++)
        {
            for (var y = 0; y < size.Y; y++)
            {
                footprint.Add(anchor + Rotate(new Vector2i(x, y), rotation));
            }
        }
    }

    private static Vector2i Rotate(Vector2i v, int rotation)
    {
        return rotation switch
        {
            1 => new Vector2i(-v.Y, v.X),
            2 => new Vector2i(-v.X, -v.Y),
            3 => new Vector2i(v.Y, -v.X),
            _ => v,
        };
    }

    /// <summary>
    /// <see cref="CEDungeonSystem.TrySpawn3DRoom"/> maps room-center-relative positions through this matrix. The
    /// translation puts the origin tile's center on <paramref name="anchor"/>'s center, so every tile center lands on
    /// a tile center whatever the size parity and rotation.
    /// </summary>
    private static Matrix3x2 GetTransform(Vector2i size, Vector2i anchor, int rotation)
    {
        var rotationMatrix = Matrix3x2.CreateRotation(rotation * MathF.PI / 2f);
        var originLocal = new Vector2(0.5f, 0.5f) - (Vector2) size / 2f;
        var translation = anchor + new Vector2(0.5f, 0.5f) - Vector2.Transform(originLocal, rotationMatrix);
        return rotationMatrix * Matrix3x2.CreateTranslation(translation);
    }
}
