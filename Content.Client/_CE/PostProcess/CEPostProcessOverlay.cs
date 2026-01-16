using Content.Shared.CCVar;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;

namespace Content.Client._CE.PostProcess;

// This overlay serves as the foundational post processing overlay.
// Ideally, for performance reasons, post processing designed to be present at all times, such as additive light blending or tonemapping, should be done as part of a single shader pass.
public sealed class CEPostProcessOverlay : Overlay
{
    [Dependency] private readonly IEntityManager _entMan = default!;
    [Dependency] private readonly ILightManager _lightManager = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    public override bool RequestScreenTexture => true;
    public override OverlaySpace Space => OverlaySpace.WorldSpace;
    private readonly ShaderInstance _basePostProcessShader;

    public CEPostProcessOverlay()
    {
        IoCManager.InjectDependencies(this);
        _basePostProcessShader = _proto.Index<ShaderPrototype>("CEPostProcess").InstanceUnique();
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        if (!_entMan.TryGetComponent(_player.LocalSession?.AttachedEntity, out EyeComponent? eyeComp))
            return false;

        if (args.Viewport.Eye != eyeComp.Eye)
            return false;

        if (!_lightManager.Enabled || !eyeComp.Eye.DrawLight || !eyeComp.Eye.DrawFov)
            return false;

        var playerEntity = _player.LocalSession?.AttachedEntity;

        if (playerEntity == null)
            return false;

        return true;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (ScreenTexture == null)
            return;

        if (args.Viewport.Eye == null)
            return;

        var worldHandle = args.WorldHandle;
        var viewport = args.WorldBounds;

        _basePostProcessShader.SetParameter("SCREEN_TEXTURE", ScreenTexture);
        _basePostProcessShader.SetParameter("LIGHT_TEXTURE", args.Viewport.LightRenderTarget.Texture);

        _basePostProcessShader.SetParameter("Zoom", args.Viewport.Eye.Zoom.X);

        worldHandle.UseShader(_basePostProcessShader);
        worldHandle.DrawRect(viewport, Color.White);
        worldHandle.UseShader(null);
    }
}

public sealed class CEPostProcessSystem : EntitySystem
{
    [Dependency] private readonly IOverlayManager _overlay = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    public override void Initialize()
    {
        base.Initialize();

        if (_cfg.GetCVar(CCVars.CEPostProcess) && !_overlay.HasOverlay<CEPostProcessOverlay>())
        {
            _overlay.AddOverlay(new CEPostProcessOverlay());
        }

        Subs.CVar(_cfg, CCVars.CEPostProcess, OnCVarUpdate, true);
    }

    private void OnCVarUpdate(bool enabled)
    {
        if (enabled && !_overlay.HasOverlay<CEPostProcessOverlay>())
        {
            _overlay.AddOverlay(new CEPostProcessOverlay());
        }
        else if (!enabled && _overlay.HasOverlay<CEPostProcessOverlay>())
        {
            _overlay.RemoveOverlay<CEPostProcessOverlay>();
        }
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _overlay.RemoveOverlay<CEPostProcessOverlay>();
    }
}
