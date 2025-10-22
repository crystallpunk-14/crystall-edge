using Content.Shared._CE.ZLevels;
using Content.Shared._CE.ZLevels.EntitySystems;
using Robust.Client.Graphics;

namespace Content.Client._CE.ZLevels;

public sealed partial class CEClientZLevelsSystem : CESharedZLevelsSystem
{
    [Dependency] private readonly IOverlayManager _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();
        _overlay.AddOverlay(new CEZLevelOverlay());
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _overlay.RemoveOverlay<CEZLevelOverlay>();
    }
}
