using Content.Shared._CE.Waypointer;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Analyzers;
using Robust.Shared.Player;

namespace Content.Client._CE.Waypointer;

/// <summary>
/// The client-side system handles initializing the overlay, as well as removing and adding it as the local player changes.
/// </summary>
public sealed partial class CEWaypointerSystem : CESharedWaypointerSystem
{
    [Dependency] private IPlayerManager  _player = default!;
    [Dependency] private IOverlayManager _overlay = default!;

    private CEWaypointerOverlay _waypointerOverlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        _waypointerOverlay = new CEWaypointerOverlay();
    }

    protected override void OnAddition(Entity<CEWaypointerComponent> player, ref ComponentInit args)
    {
        base.OnAddition(player, ref args);

        if (_player.LocalEntity == null || player.Owner != _player.LocalEntity.Value)
            return;

        _overlay.AddOverlay(_waypointerOverlay);
    }

    protected override void OnRemoval(Entity<CEWaypointerComponent> player, ref ComponentRemove args)
    {
        base.OnRemoval(player, ref args);

        if (_player.LocalEntity == null || player.Owner != _player.LocalEntity.Value)
            return;

        _overlay.RemoveOverlay(_waypointerOverlay);
    }

    [SubscribeLocalEvent]
    private void OnPlayerAttached(Entity<CEWaypointerComponent> mob, ref LocalPlayerAttachedEvent args)
    {
        if (args.Entity != _player.LocalEntity)
            return;

        _overlay.AddOverlay(_waypointerOverlay);
    }

    [SubscribeLocalEvent]
    private void OnPlayerDetached(Entity<CEWaypointerComponent> mob, ref LocalPlayerDetachedEvent args)
    {
        if (args.Entity != _player.LocalEntity)
            return;

        _overlay.RemoveOverlay(_waypointerOverlay);
    }
}
