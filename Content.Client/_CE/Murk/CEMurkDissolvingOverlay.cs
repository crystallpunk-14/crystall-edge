using Content.Shared._CE.Murk.Components;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client._CE.Murk;

/// <summary>
/// Drains the color out of the local player's vision the more dissolved they are.
/// </summary>
public sealed partial class CEMurkDissolvingOverlay : Overlay
{
    [Dependency] private IEntityManager _entityManager = default!;
    [Dependency] private IPrototypeManager _prototypeManager = default!;
    [Dependency] private IPlayerManager _playerManager = default!;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;
    public override bool RequestScreenTexture => true;

    private readonly ShaderInstance _desaturateShader;

    private readonly ProtoId<ShaderPrototype> _desaturateShaderProto = "CEMurkDesaturate";

    /// <summary>
    /// Dissolution level at which the screen goes fully black and white.
    /// </summary>
    private const float FullyDesaturatedAt = 0.8f;

    public float CurrentDissolved; // between 0 and 1
    private float _visualScale = 0; // between 0 and 1

    public CEMurkDissolvingOverlay()
    {
        IoCManager.InjectDependencies(this);
        _desaturateShader = _prototypeManager.Index(_desaturateShaderProto).InstanceUnique();

        ZIndex = 9; // same as the noir overlay - over the damage/rainbow overlays, before the black and white shader
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        var playerEntity = _playerManager.LocalEntity;

        if (playerEntity == null)
            return;

        if (!_entityManager.TryGetComponent<CEMurkDissolvingComponent>(playerEntity, out var dissolving)
            || !dissolving.Enabled)
        {
            CurrentDissolved = 0f;
            return;
        }

        CurrentDissolved = dissolving.Dissolved;
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        if (!_entityManager.TryGetComponent(_playerManager.LocalEntity, out EyeComponent? eyeComp))
            return false;

        if (args.Viewport.Eye != eyeComp.Eye)
            return false;

        _visualScale = Math.Clamp(CurrentDissolved / FullyDesaturatedAt, 0.0f, 1.0f);
        return _visualScale > 0;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (ScreenTexture == null)
            return;

        var handle = args.WorldHandle;
        _desaturateShader.SetParameter("SCREEN_TEXTURE", ScreenTexture);
        _desaturateShader.SetParameter("Strength", _visualScale);
        handle.UseShader(_desaturateShader);
        handle.DrawRect(args.WorldBounds, Color.White);
        handle.UseShader(null);
    }
}
