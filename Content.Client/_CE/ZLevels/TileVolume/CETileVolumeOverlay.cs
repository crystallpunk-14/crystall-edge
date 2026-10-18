using System.Numerics;
using Content.Client.Parallax;
using Content.Client.Resources;
using Content.Shared.Maps;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Client._CE.ZLevels.TileVolume;

/// <summary>
/// Gives z-level floors visual thickness: under every tile edge that borders an opening and faces
/// screen-down, draws a thin lip from <see cref="ContentTileDefinition.VolumeSprite"/>.
/// Runs in every z-level render pass, each pass drawing only the grids of its own map.
/// Camera rotation is assumed to snap to 90 degrees, so only one edge per tile is ever visible.
/// </summary>
public sealed partial class CETileVolumeOverlay : Overlay
{
    [Dependency] private IEntityManager _entity = default!;
    [Dependency] private IResourceCache _resource = default!;
    [Dependency] private ITileDefinitionManager _tileDef = default!;

    private readonly CEClientTileVolumeSystem _volume;
    private readonly SharedMapSystem _map;
    private readonly SharedTransformSystem _transform;

    private List<Entity<MapGridComponent>> _grids = new();

    /// <summary>
    /// Resolved lip sprites indexed by tile type id, filled lazily.
    /// </summary>
    private VolumeSprite?[] _sprites = [];

    private const int VariantWidth = EyeManager.PixelsPerMeter;

    // Below grids, so transparent tiles (glass) draw over the lip instead of the lip covering them.
    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowWorld;

    public CETileVolumeOverlay()
    {
        // After parallax, which shares this space on the lowest z-level and would otherwise cover the lip.
        ZIndex = ParallaxSystem.ParallaxZIndex + 1;
        IoCManager.InjectDependencies(this);

        _volume = _entity.System<CEClientTileVolumeSystem>();
        _map = _entity.System<SharedMapSystem>();
        _transform = _entity.System<SharedTransformSystem>();
    }

    public void ClearVolumeSprites()
    {
        Array.Clear(_sprites);
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        return args.MapId != MapId.Nullspace && args.Viewport.Eye != null;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var handle = args.WorldHandle;
        var screenDown = (-args.Viewport.Eye!.Rotation).RotateVec(new Vector2(0, -1));

        _grids.Clear();
        _map.FindGridsIntersecting(args.MapId, args.WorldAABB, ref _grids, approx: true, includeMap: true);

        foreach (var grid in _grids)
        {
            var cache = _volume.EnsureCache(grid);
            var (_, worldRot, worldMatrix, invMatrix) = _transform.GetWorldPositionRotationMatrixWithInv(grid.Owner);

            var dir = CETileVolumeDirection.FromVector((-worldRot).RotateVec(screenDown));
            var angle = CETileVolumeDirection.Angles[dir];
            var tileSize = grid.Comp.TileSize;

            // Enlarged by a tile so lips of tiles just outside the view, which hang into it, still draw.
            var localBounds = invMatrix.TransformBox(args.WorldBounds).Enlarged(tileSize);
            var chunkWorldSize = (float) CETileVolumeChunk.Size * tileSize;
            var startX = (int) MathF.Floor(localBounds.Left / chunkWorldSize);
            var startY = (int) MathF.Floor(localBounds.Bottom / chunkWorldSize);
            var endX = (int) MathF.Floor(localBounds.Right / chunkWorldSize);
            var endY = (int) MathF.Floor(localBounds.Top / chunkWorldSize);

            handle.SetTransform(worldMatrix);

            for (var chunkY = startY; chunkY <= endY; chunkY++)
            {
                for (var chunkX = startX; chunkX <= endX; chunkX++)
                {
                    var chunkIndices = new Vector2i(chunkX, chunkY);
                    var chunk = _volume.GetChunk(grid, cache, chunkIndices);
                    var mask = chunk.Masks[dir];
                    if (mask == 0)
                        continue;

                    var origin = chunkIndices * CETileVolumeChunk.Size;
                    while (mask != 0)
                    {
                        var bitIndex = BitOperations.TrailingZeroCount(mask);
                        mask &= mask - 1;

                        var tile = origin + new Vector2i(bitIndex % CETileVolumeChunk.Size, bitIndex / CETileVolumeChunk.Size);
                        DrawLip(handle, GetSprite(chunk.TileIds![bitIndex]), tile, tileSize, angle);
                    }
                }
            }
        }

        handle.SetTransform(Matrix3x2.Identity);
    }

    private static void DrawLip(DrawingHandleWorld handle, VolumeSprite sprite, Vector2i tile, ushort tileSize, Angle angle)
    {
        var left = tile.X * tileSize;
        var bottom = tile.Y * tileSize;
        var center = new Vector2(left + tileSize / 2f, bottom + tileSize / 2f);

        // Authored for the south edge: hangs below the tile, texture top touching the edge.
        // Other edges reuse it rotated around the tile center, which keeps it upright on screen.
        var box = new Box2(left, bottom - sprite.Thickness * tileSize, left + tileSize, bottom);

        var variant = sprite.Variants == 1 ? 0 : VariantHash(tile) % sprite.Variants;
        var region = new UIBox2(variant * VariantWidth, 0, (variant + 1) * VariantWidth, sprite.Texture.Height);

        handle.DrawTextureRectRegion(sprite.Texture, new Box2Rotated(box, angle, center), subRegion: region);
    }

    private static int VariantHash(Vector2i tile)
    {
        return ((tile.X * 73856093) ^ (tile.Y * 19349663)) & int.MaxValue;
    }

    private VolumeSprite GetSprite(int tileId)
    {
        if (tileId >= _sprites.Length)
            Array.Resize(ref _sprites, tileId + 1);

        if (_sprites[tileId] is { } cached)
            return cached;

        var def = (ContentTileDefinition) _tileDef[tileId];
        var texture = _resource.GetTexture(def.VolumeSprite);
        var sprite = new VolumeSprite(
            texture,
            Math.Max(1, texture.Width / VariantWidth),
            texture.Height / (float) EyeManager.PixelsPerMeter);

        _sprites[tileId] = sprite;
        return sprite;
    }

    private sealed record VolumeSprite(Texture Texture, int Variants, float Thickness);
}
