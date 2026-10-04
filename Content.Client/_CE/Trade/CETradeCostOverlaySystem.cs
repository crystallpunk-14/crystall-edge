using Robust.Client.Graphics;

namespace Content.Client._CE.Trade;

public sealed partial class CETradeCostOverlaySystem : EntitySystem
{
    [Dependency] private IOverlayManager _overlayMan = default!;

    private CETradeCostOverlay _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        _overlay = new CETradeCostOverlay();
        _overlayMan.AddOverlay(_overlay);
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _overlayMan.RemoveOverlay(_overlay);
    }
}
