using Content.Shared._CE.Murk.Systems;
using Robust.Client.Graphics;

namespace Content.Client._CE.Murk;

public sealed partial class CEClientMurkSystem : CESharedMurkSystem
{
    [Dependency] private readonly IOverlayManager _overlayMgr = default!;

    public override void Initialize()
    {
        base.Initialize();

        _overlayMgr.AddOverlay(new CEMurkOverlay());
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _overlayMgr.RemoveOverlay<CEMurkOverlay>();
    }
}
