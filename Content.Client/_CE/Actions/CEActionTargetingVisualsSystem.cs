using Robust.Client.Graphics;

namespace Content.Client._CE.Actions;

/// <summary>
/// Manages <see cref="CEActionTargetingOverlay"/> lifetime:
/// adds it on Initialize, removes on Shutdown.
/// The overlay itself reads <see cref="ActionUIController.SelectingTargetFor"/>
/// every frame to decide what to draw.
/// </summary>
public sealed partial class CEActionTargetingVisualsSystem : EntitySystem
{
    [Dependency] private IOverlayManager _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();
        _overlay.AddOverlay(new CEActionTargetingOverlay());
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _overlay.RemoveOverlay<CEActionTargetingOverlay>();
    }
}
