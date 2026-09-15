using Robust.Client.Graphics;

namespace Content.Client._CE.ZLevels.Lighting;

public sealed partial class CEZLevelLightSystem : EntitySystem
{
    [Dependency] private IOverlayManager _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        _overlay.AddOverlay(new CEZLevelLightOverlay(EntityManager));
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _overlay.RemoveOverlay<CEZLevelLightOverlay>();
    }
}
