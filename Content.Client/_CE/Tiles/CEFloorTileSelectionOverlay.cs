using System.Numerics;
using Content.Client.Hands.Systems;
using Content.Client.Resources;
using Content.Client.Viewport;
using Content.Shared.Interaction;
using Content.Shared.Maps;
using Content.Shared.Tiles;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Client.ResourceManagement;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client._CE.Tiles;

/// <summary>
/// Overlay that displays a sprite over the tile the cursor is hovering over
/// when the player is holding an item with FloorTileComponent
/// </summary>
public sealed class CEFloorTileSelectionOverlay : Overlay
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IInputManager _inputManager = default!;
    [Dependency] private readonly IEyeManager _eyeManager = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly ITileDefinitionManager _tileDefinitionManager = default!;

    private readonly SpriteSystem _sprite;
    private readonly SharedMapSystem _mapSystem;
    private readonly HandsSystem _handsSystem;
    private readonly FloorTileSystem _floorTileSystem;
    private readonly SharedInteractionSystem _interactionSystem;

    private readonly Texture _texture;

    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowEntities;

    public CEFloorTileSelectionOverlay()
    {
        IoCManager.InjectDependencies(this);

        _mapSystem = _entityManager.System<SharedMapSystem>();
        _handsSystem = _entityManager.System<HandsSystem>();
        _floorTileSystem = _entityManager.System<FloorTileSystem>();
        _interactionSystem = _entityManager.System<SharedInteractionSystem>();
        _sprite = _entityManager.System<SpriteSystem>();

        _texture = _sprite.Frame0(
            new SpriteSpecifier.Rsi(new ResPath("/Textures/_CE/Markers/biome.rsi"), "frame"));
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        return args.Viewport.Eye is not ScalingViewport.ZEye;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var worldHandle = args.WorldHandle;

        // Get local player entity
        if (_playerManager.LocalSession?.AttachedEntity is not { } player)
            return;

        // Get active hand item with FloorTileComponent
        var activeItem = _handsSystem.GetActiveItem(player);
        if (activeItem == null || !_entityManager.TryGetComponent<FloorTileComponent>(activeItem.Value, out var floorTile))
            return;

        // Check if FloorTileComponent has valid outputs
        if (floorTile.Outputs == null || floorTile.Outputs.Count == 0)
            return;

        // Get mouse screen position
        var mouseScreenPos = _inputManager.MouseScreenPosition;

        // Convert to map coordinates
        var mouseMapPos = _eyeManager.PixelToMap(mouseScreenPos);

        //Check if the tile is in interaction range and unobstructed
        if (!_interactionSystem.InRangeUnobstructed(player, mouseMapPos))
            return;

        if (mouseMapPos.MapId == MapId.Nullspace)
            return;

        // Try to find grid at mouse position
        if (!_mapManager.TryFindGridAt(mouseMapPos, out var gridUid, out var grid))
            return;

        // Get tile indices at mouse position
        var tileIndices = _mapSystem.TileIndicesFor(gridUid, grid, mouseMapPos);

        // Get current tile at position
        var currentTile = _mapSystem.GetTileRef(gridUid, grid, tileIndices);
        var currentTileDef = (ContentTileDefinition)_tileDefinitionManager[currentTile.Tile.TypeId];

        // Check if any of the output tiles can be placed on the current tile
        var canPlace = false;
        foreach (var output in floorTile.Outputs)
        {
            if (!_proto.Resolve(output, out var targetTileDef))
                continue;

            // Check if this tile can be placed on the current tile's baseTurf
            if (_floorTileSystem.HasBaseTurf(targetTileDef, currentTileDef.ID))
            {
                canPlace = true;
                break;
            }
        }

        // Get tile center position in world coordinates
        var tileCenter = _mapSystem.GridTileToWorld(gridUid, grid, tileIndices);

        // Offset to center of the tile (GridTileToWorld returns bottom-left corner)
        var tileCenterOffset = tileCenter.Position - new Vector2(grid.TileSize / 2f, grid.TileSize / 2f);

        // Draw sprite centered on the tile
        // Red if can't place, white with transparency if can place
        var color = canPlace ? Color.White.WithAlpha(0.7f) : Color.Red.WithAlpha(0.7f);
        worldHandle.DrawTexture(_texture, tileCenterOffset, color);
    }
}
