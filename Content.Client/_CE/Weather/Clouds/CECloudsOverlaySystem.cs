using Robust.Client.Graphics;

namespace Content.Client._CE.Weather.Clouds;

/// <summary>
/// Owns the lifecycle of <see cref="CECloudsOverlay"/>. The overlay itself decides per-map
/// whether to draw anything, based on the presence of <see cref="Content.Shared._CE.Weather.Clouds.CECloudsOverlayComponent"/>.
/// </summary>
public sealed partial class CECloudsOverlaySystem : EntitySystem
{
    [Dependency] private IOverlayManager _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();
        _overlay.AddOverlay(new CECloudsOverlay());
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _overlay.RemoveOverlay<CECloudsOverlay>();
    }
}
