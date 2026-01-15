using Content.Client.Hands.Systems;
using Content.Shared.Hands;
using Content.Shared.Tiles;
using Robust.Client.Graphics;
using Robust.Client.Player;

namespace Content.Client._CE.Tiles;

/// <summary>
/// System for displaying a pickaxe sprite overlay over tiles when holding an item with FloorTileComponent
/// </summary>
public sealed class CEFloorTileSelectionSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IOverlayManager _overlayManager = default!;

    private CEFloorTileSelectionOverlay? _overlay;

    public override void Initialize()
    {
        base.Initialize();

        // Subscribe to events when items are equipped/unequipped in hands
        SubscribeLocalEvent<FloorTileComponent, HandSelectedEvent>(OnHandSelected);
        SubscribeLocalEvent<FloorTileComponent, HandDeselectedEvent>(OnHandDeselected);
        SubscribeLocalEvent<FloorTileComponent, GotEquippedHandEvent>(OnEquipped);
        SubscribeLocalEvent<FloorTileComponent, GotUnequippedHandEvent>(OnUnequipped);
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

    public override void Shutdown()
    {
        base.Shutdown();

        // Clean up overlay on shutdown
        if (_overlay != null)
        {
            _overlayManager.RemoveOverlay(_overlay);
            _overlay = null;
        }
    }
}
