using Content.Shared._CE.EnergyScanner;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Player;

namespace Content.Client._CE.EnergyScanner;

/// <summary>
/// Shows <see cref="CEEnergyScannerOverlay"/> while the local player has <see cref="CEEnergyScannerViewerComponent"/>.
/// </summary>
public sealed partial class CEClientEnergyScannerSystem : EntitySystem
{
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private IOverlayManager _overlayMan = default!;

    private CEEnergyScannerOverlay? _overlay;

    public override void Shutdown()
    {
        base.Shutdown();
        RemoveOverlay();
    }

    [SubscribeLocalEvent]
    private void OnStartup(Entity<CEEnergyScannerViewerComponent> ent, ref ComponentStartup args)
    {
        if (ent.Owner == _player.LocalEntity)
            AddOverlay();
    }

    [SubscribeLocalEvent]
    private void OnShutdown(Entity<CEEnergyScannerViewerComponent> ent, ref ComponentShutdown args)
    {
        if (ent.Owner == _player.LocalEntity)
            RemoveOverlay();
    }

    [SubscribeLocalEvent]
    private void OnPlayerAttached(LocalPlayerAttachedEvent args)
    {
        if (HasComp<CEEnergyScannerViewerComponent>(args.Entity))
            AddOverlay();
    }

    [SubscribeLocalEvent]
    private void OnPlayerDetached(LocalPlayerDetachedEvent args)
    {
        RemoveOverlay();
    }

    private void AddOverlay()
    {
        if (_overlay != null)
            return;

        _overlay = new CEEnergyScannerOverlay();
        _overlayMan.AddOverlay(_overlay);
    }

    private void RemoveOverlay()
    {
        if (_overlay == null)
            return;

        _overlayMan.RemoveOverlay(_overlay);
        _overlay = null;
    }
}
