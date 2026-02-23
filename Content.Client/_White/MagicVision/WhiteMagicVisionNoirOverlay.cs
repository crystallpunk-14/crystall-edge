using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;

namespace Content.Client._White.MagicVision;

public sealed class WhiteMagicVisionNoirOverlay : Overlay
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;
    public override bool RequestScreenTexture => true;
    private readonly ShaderInstance _noirShader;

    public WhiteMagicVisionNoirOverlay()
    {
        IoCManager.InjectDependencies(this);
        _noirShader = _prototypeManager.Index<ShaderPrototype>("WhiteBlueNoir").InstanceUnique();
        ZIndex = 9; // draw this over the DamageOverlay, RainbowOverlay etc, but before the black and white shader
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (ScreenTexture == null)
            return;

        var handle = args.WorldHandle;
        _noirShader.SetParameter("SCREEN_TEXTURE", ScreenTexture);
        handle.UseShader(_noirShader);
        handle.DrawRect(args.WorldBounds, Color.White);
        handle.UseShader(null);
    }
}
