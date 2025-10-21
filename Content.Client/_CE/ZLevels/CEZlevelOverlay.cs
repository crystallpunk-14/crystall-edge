using Content.Client.Viewport;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Client._CE.ZLevels;

public sealed class CEZLevelOverlay : Overlay
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    private readonly ShaderInstance? _blurShader;

    public override bool RequestScreenTexture => true;
    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    public CEZLevelOverlay()
    {
        ZIndex = 0;
        IoCManager.InjectDependencies(this);

        _blurShader = _proto.Index<ShaderPrototype>("CEZBlur").InstanceUnique();
        ZIndex = 102;
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        //if (args.MapId != MapId.Nullspace && _entManager.HasComponent<ZStackMemberComponent>(_mapManager.GetMapEntityId(args.MapId)))
        //    return true;

        return false;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (args.MapId == MapId.Nullspace)
            return;

        if (ScreenTexture == null)
            return;

        var worldHandle = args.WorldHandle;

        worldHandle.DrawRect(args.WorldAABB, Color.Black.WithAlpha(0.5f));

        if (args.Viewport.Eye is not ScalingViewport.ZEye)
            return;

        worldHandle.UseShader(_blurShader);
        worldHandle.DrawRect(args.WorldAABB, Color.White);
        worldHandle.UseShader(null);
    }
}
