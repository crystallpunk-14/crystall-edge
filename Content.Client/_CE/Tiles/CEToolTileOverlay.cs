using System.Numerics;
using Content.Client.Gameplay;
using Content.Client.Hands.Systems;
using Content.Client.Resources;
using Content.Client.Viewport;
using Content.Shared.Interaction;
using Content.Shared.Maps;
using Content.Shared.Tools.Components;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Client.ResourceManagement;
using Robust.Client.State;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Utility;

namespace Content.Client._CE.Tiles;

/// <summary>
/// Overlay that displays a sprite over the tile the cursor is hovering over
/// when the player is holding a tool with ToolTileCompatibleComponent
/// </summary>
public sealed class CEToolTileOverlay : Overlay
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IInputManager _inputManager = default!;
    [Dependency] private readonly IEyeManager _eyeManager = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly ITileDefinitionManager _tileDefinitionManager = default!;
    [Dependency] private readonly IStateManager _stateManager = default!;

    private readonly SpriteSystem _sprite;
    private readonly SharedMapSystem _mapSystem;
    private readonly HandsSystem _handsSystem;
    private readonly SharedInteractionSystem _interactionSystem;

    private readonly Texture _texture;

    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowEntities;

    public CEToolTileOverlay()
    {
        IoCManager.InjectDependencies(this);

        _mapSystem = _entityManager.System<SharedMapSystem>();
        _handsSystem = _entityManager.System<HandsSystem>();
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

        // Get active hand item with ToolTileCompatibleComponent and ToolComponent
        var activeItem = _handsSystem.GetActiveItem(player);
        if (activeItem == null ||
            !_entityManager.TryGetComponent<ToolTileCompatibleComponent>(activeItem.Value, out var toolTileComp) ||
            !_entityManager.TryGetComponent<ToolComponent>(activeItem.Value, out var toolComp))
            return;

        // Get mouse screen position
        var mouseScreenPos = _inputManager.MouseScreenPosition;

        // Convert to map coordinates
        var mouseMapPos = _eyeManager.PixelToMap(mouseScreenPos);

        // Check if the tile is in interaction range and unobstructed
        if (!_interactionSystem.InRangeUnobstructed(player, mouseMapPos))
            return;

        if (mouseMapPos.MapId == MapId.Nullspace)
            return;

        // Try to find grid at mouse position
        if (!_mapManager.TryFindGridAt(mouseMapPos, out var gridUid, out var grid))
            return;

        // Check if there is any entity under cursor using the same method as InteractionOutline
        // Don't show overlay if there are entities at the mouse position
        if (_stateManager.CurrentState is GameplayStateBase screen)
        {
            var entityUnderCursor = screen.GetClickedEntity(mouseMapPos);
            if (entityUnderCursor != null && entityUnderCursor != player && entityUnderCursor != gridUid)
                return;
        }

        // Get tile indices at mouse position
        var tileIndices = _mapSystem.TileIndicesFor(gridUid, grid, mouseMapPos);

        // Get tile center position in world coordinates
        var tileCenter = _mapSystem.GridTileToWorld(gridUid, grid, tileIndices);

        // Get current tile at position
        var currentTile = _mapSystem.GetTileRef(gridUid, grid, tileIndices);
        var currentTileDef = (ContentTileDefinition)_tileDefinitionManager[currentTile.Tile.TypeId];

        // Check if the tool can deconstruct this tile
        // Tool can work if it has any of the required deconstruct tools AND tile has baseTurf
        var qualities = toolComp.Qualities;
        var canDeconstruct = qualities.ContainsAny(currentTileDef.DeconstructTools);

        // Offset to center of the tile (GridTileToWorld returns bottom-left corner)
        var tileCenterOffset = tileCenter.Position - new Vector2(grid.TileSize / 2f, grid.TileSize / 2f);

        // Draw sprite centered on the tile
        // White if can deconstruct, red if can't
        var color = canDeconstruct ? Color.White.WithAlpha(0.7f) : Color.Red.WithAlpha(0.7f);
        worldHandle.DrawTexture(_texture, tileCenterOffset, color);
    }
}
