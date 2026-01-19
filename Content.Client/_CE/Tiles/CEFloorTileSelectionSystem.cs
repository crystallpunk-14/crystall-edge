using Content.Client.Hands.Systems;
using Content.Shared.Hands;
using Content.Shared.Tiles;
using Content.Shared.Tools.Components;
using Robust.Client.Graphics;
using Robust.Client.Player;

namespace Content.Client._CE.Tiles;

/// <summary>
/// System for displaying overlay sprites over tiles when holding items with FloorTileComponent or ToolTileCompatibleComponent
/// </summary>
public sealed class CEFloorTileSelectionSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IOverlayManager _overlayManager = default!;

    private CEFloorTileSelectionOverlay? _overlay;
    private CEToolTileOverlay? _toolOverlay;

    public override void Initialize()
    {
        base.Initialize();

        // Subscribe to events for floor tile component
        SubscribeLocalEvent<FloorTileComponent, HandSelectedEvent>(OnHandSelected);
        SubscribeLocalEvent<FloorTileComponent, HandDeselectedEvent>(OnHandDeselected);
        SubscribeLocalEvent<FloorTileComponent, GotEquippedHandEvent>(OnEquipped);
        SubscribeLocalEvent<FloorTileComponent, GotUnequippedHandEvent>(OnUnequipped);

        // Subscribe to events for tool tile compatible component
        SubscribeLocalEvent<ToolTileCompatibleComponent, HandSelectedEvent>(OnToolHandSelected);
        SubscribeLocalEvent<ToolTileCompatibleComponent, HandDeselectedEvent>(OnToolHandDeselected);
        SubscribeLocalEvent<ToolTileCompatibleComponent, GotEquippedHandEvent>(OnToolEquipped);
        SubscribeLocalEvent<ToolTileCompatibleComponent, GotUnequippedHandEvent>(OnToolUnequipped);
    }

    private void OnHandSelected(Entity<FloorTileComponent> ent, ref HandSelectedEvent args)
    {
        if (!IsLocalPlayer(args.User))
            return;

        UpdateOverlay(args.User);
    }

    private void OnHandDeselected(Entity<FloorTileComponent> ent, ref HandDeselectedEvent args)
    {
        if (!IsLocalPlayer(args.User))
            return;

        UpdateOverlay(args.User);
    }

    private void OnUnequipped(Entity<FloorTileComponent> ent, ref GotUnequippedHandEvent args)
    {
        if (!IsLocalPlayer(args.User))
            return;

        UpdateOverlay(args.User);
    }

    private void OnEquipped(Entity<FloorTileComponent> ent, ref GotEquippedHandEvent args)
    {
        if (!IsLocalPlayer(args.User))
            return;

        UpdateOverlay(args.User);
    }

    // Tool tile compatible event handlers
    private void OnToolHandSelected(Entity<ToolTileCompatibleComponent> ent, ref HandSelectedEvent args)
    {
        if (!IsLocalPlayer(args.User))
            return;

        UpdateToolOverlay(args.User);
    }

    private void OnToolHandDeselected(Entity<ToolTileCompatibleComponent> ent, ref HandDeselectedEvent args)
    {
        if (!IsLocalPlayer(args.User))
            return;

        UpdateToolOverlay(args.User);
    }

    private void OnToolUnequipped(Entity<ToolTileCompatibleComponent> ent, ref GotUnequippedHandEvent args)
    {
        if (!IsLocalPlayer(args.User))
            return;

        UpdateToolOverlay(args.User);
    }

    private void OnToolEquipped(Entity<ToolTileCompatibleComponent> ent, ref GotEquippedHandEvent args)
    {
        if (!IsLocalPlayer(args.User))
            return;

        UpdateToolOverlay(args.User);
    }

    private bool IsLocalPlayer(EntityUid entity)
    {
        return _playerManager.LocalSession?.AttachedEntity == entity;
    }

    private void UpdateOverlay(EntityUid player)
    {
        // Get active hand item
        var handsSystem = EntityManager.System<HandsSystem>();
        var activeItem = handsSystem.GetActiveItem(player);

        // Check if active item has FloorTileComponent
        var hasFloorTile = activeItem != null && HasComp<FloorTileComponent>(activeItem.Value);

        // Manage overlay state
        if (hasFloorTile && _overlay == null)
        {
            // Add overlay if player is holding a floor tile item
            _overlay = new CEFloorTileSelectionOverlay();
            _overlayManager.AddOverlay(_overlay);
        }
        else if (!hasFloorTile && _overlay != null)
        {
            // Remove overlay if player is no longer holding a floor tile item
            _overlayManager.RemoveOverlay(_overlay);
            _overlay = null;
        }
    }

    private void UpdateToolOverlay(EntityUid player)
    {
        // Get active hand item
        var handsSystem = EntityManager.System<HandsSystem>();
        var activeItem = handsSystem.GetActiveItem(player);

        // Check if active item has ToolTileCompatibleComponent
        var hasTool = activeItem != null && HasComp<ToolTileCompatibleComponent>(activeItem.Value);

        // Manage tool overlay state
        if (hasTool && _toolOverlay == null)
        {
            // Add overlay if player is holding a tool with ToolTileCompatibleComponent
            _toolOverlay = new CEToolTileOverlay();
            _overlayManager.AddOverlay(_toolOverlay);
        }
        else if (!hasTool && _toolOverlay != null)
        {
            // Remove overlay if player is no longer holding a compatible tool
            _overlayManager.RemoveOverlay(_toolOverlay);
            _toolOverlay = null;
        }
    }

    public override void Shutdown()
    {
        base.Shutdown();

        // Clean up overlays on shutdown
        if (_overlay != null)
        {
            _overlayManager.RemoveOverlay(_overlay);
            _overlay = null;
        }

        if (_toolOverlay != null)
        {
            _overlayManager.RemoveOverlay(_toolOverlay);
            _toolOverlay = null;
        }
    }
}
