using System.Numerics;
using Content.Client._CE.ZLevels.Core;
using Content.Client.Gameplay;
using Content.Client.Hands.Systems;
using Content.Client.Resources;
using Content.Client.Viewport;
using Content.Shared._CE.ZLevels.Core.Components;
using Content.Shared._CE.ZLevels.Core.EntitySystems;
using Content.Shared._CE.ZLevels.Tiles;
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
public sealed partial class CEToolTileOverlay : Overlay
{
    [Dependency] private IEntityManager _entityManager = default!;
    [Dependency] private IInputManager _inputManager = default!;
    [Dependency] private IEyeManager _eyeManager = default!;
    [Dependency] private IPlayerManager _playerManager = default!;
    [Dependency] private ITileDefinitionManager _tileDefinitionManager = default!;
    [Dependency] private IStateManager _stateManager = default!;

    private readonly SpriteSystem _sprite;
    private readonly SharedMapSystem _mapSystem;
    private readonly HandsSystem _handsSystem;
    private readonly SharedInteractionSystem _interactionSystem;
    private readonly CEClientZLevelsSystem _zLevel;

    private readonly Texture _texture;

    // Normally drawn below entities, like a floor decal. While targeting the ceiling (level above)
    // it needs to draw above entities instead, so it reads as being overhead rather than underfoot -
    // WorldSpaceBelowFOV still keeps it subject to lighting/FOV like a normal world sprite.
    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowEntities | OverlaySpace.WorldSpaceBelowFOV;

    public CEToolTileOverlay()
    {
        IoCManager.InjectDependencies(this);

        _mapSystem = _entityManager.System<SharedMapSystem>();
        _handsSystem = _entityManager.System<HandsSystem>();
        _interactionSystem = _entityManager.System<SharedInteractionSystem>();
        _sprite = _entityManager.System<SpriteSystem>();
        _zLevel = _entityManager.System<CEClientZLevelsSystem>();

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
        if (!_mapSystem.TryFindGridAt(mouseMapPos, out var gridUid, out var grid))
            return;

        // Check if there is any entity under cursor using the same method as InteractionOutline
        // Don't show overlay if there are entities at the mouse position
        if (_stateManager.CurrentState is GameplayStateBase screen)
        {
            var entityUnderCursor = screen.GetClickedEntity(mouseMapPos);
            if (entityUnderCursor != null && entityUnderCursor != player && entityUnderCursor != gridUid)
                return;
        }

        // Looking up with a ceiling-capable tool: target the tile on the level above instead of the
        // one underfoot, draw above entities instead of below, and visually raise the sprite by
        // CESharedZLevelsSystem.ZLevelOffset so it reads as the ceiling. The raise vector is
        // counter-rotated against the eye the same way ScalingViewport.CEZLevels.cs offsets the
        // actual z-level-above render pass, so it stays screen-up no matter how the camera is rotated.
        var ceilingMode = _entityManager.TryGetComponent<CEZLevelViewerComponent>(player, out var viewer) &&
                           viewer.LookUp &&
                           _entityManager.HasComponent<CEZLevelToolTileComponent>(activeItem.Value);

        var wantedSpace = ceilingMode ? OverlaySpace.WorldSpaceBelowFOV : OverlaySpace.WorldSpaceBelowEntities;
        if (args.Space != wantedSpace)
            return;

        var raiseOffset = Vector2.Zero;
        if (ceilingMode)
        {
            if (!_entityManager.TryGetComponent<TransformComponent>(player, out var playerXform) ||
                playerXform.MapUid is not { } mapUid ||
                !_zLevel.TryMapUp((mapUid, null), out var aboveMap) ||
                !_mapSystem.TryFindGridAt(aboveMap.Owner, mouseMapPos.Position, out gridUid, out grid))
                return;

            Angle rotation = (args.Viewport.Eye?.Rotation ?? Angle.Zero) * -1;
            var offset = rotation.ToWorldVec() * CESharedZLevelsSystem.ZLevelOffset;
            raiseOffset = -offset;
        }

        // Get tile indices at mouse position
        var tileIndices = _mapSystem.WorldToTile(gridUid, grid, mouseMapPos.Position);

        // Get tile center position in world coordinates
        var tileCenter = _mapSystem.GridTileToWorld(gridUid, grid, tileIndices);

        // Get current tile at position
        var currentTile = _mapSystem.GetTileRef(gridUid, grid, tileIndices);
        var currentTileDef = (ContentTileDefinition)_tileDefinitionManager[currentTile.Tile.TypeId];

        // Check if the tool can deconstruct this tile
        // Tool can work if it has any of the required deconstruct tools AND tile has baseTurf
        var qualities = toolComp.Qualities;
        var canDeconstruct = qualities.ContainsAny(currentTileDef.DeconstructTools);

        // Offset to center of the tile (GridTileToWorld returns bottom-left corner), plus the
        // ceiling raise offset (zero when not targeting the level above)
        var tileCenterOffset = tileCenter.Position - new Vector2(grid.TileSize / 2f, grid.TileSize / 2f) + raiseOffset;

        // Draw sprite centered on the tile
        // White if can deconstruct, red if can't
        var color = canDeconstruct ? Color.White.WithAlpha(0.7f) : Color.Red.WithAlpha(0.7f);
        worldHandle.DrawTexture(_texture, tileCenterOffset, color);
    }
}
