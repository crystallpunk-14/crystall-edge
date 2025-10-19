using Content.Shared._CE.Murk.Components;
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

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        var query = EntityQueryEnumerator<CEMurkSourceComponent>();
        while (query.MoveNext(out var uid, out var murk))
        {
            murk.LerpedIntensity = MathHelper.Lerp(murk.LerpedIntensity, murk.Intensity, 0.01f);
        }

        var mapQuery = EntityQueryEnumerator<CEMurkedMapComponent>();
        while (mapQuery.MoveNext(out var uid, out var murkedMap))
        {
            murkedMap.LerpedIntensity = MathHelper.Lerp(murkedMap.LerpedIntensity, murkedMap.Intensity, 0.01f);
        }
    }
}
