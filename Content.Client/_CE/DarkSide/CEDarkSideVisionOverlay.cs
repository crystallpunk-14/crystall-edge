using Content.Client._CE.ZLevels.Core;
using Content.Client.Viewport;
using Content.Shared._CE.ZLevels.Core.Components;
using Content.Shared.CCVar;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client._CE.DarkSide;

/// <summary>
/// Full-screen view from the dark side. Fades in while <see cref="Active"/> and back out once it is cleared.
/// </summary>
public sealed partial class CEDarkSideVisionOverlay : Overlay
{
    [Dependency] private IConfigurationManager _config = default!;
    [Dependency] private IEntityManager _entityManager = default!;
    [Dependency] private IPrototypeManager _prototypeManager = default!;
    [Dependency] private IPlayerManager _playerManager = default!;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;
    public override bool RequestScreenTexture => true;

    private static readonly ProtoId<ShaderPrototype> ShaderProto = "CEDarkSide";

    /// <summary>
    /// How long a full fade in or out takes.
    /// </summary>
    private const float FadeSeconds = 1.5f;

    private readonly ShaderInstance _shader;

    private CEClientZLevelsSystem? _zLevels;

    private float _wind = 1f;
    private float _strength;

    /// <summary>
    /// Whether the effect is fading in (or fully shown), rather than fading out.
    /// </summary>
    public bool Active = true;

    /// <summary>
    /// Whether the effect has fully faded out and the overlay can be removed.
    /// </summary>
    public bool FadedOut => !Active && _strength <= 0f;

    public CEDarkSideVisionOverlay()
    {
        IoCManager.InjectDependencies(this);
        _shader = _prototypeManager.Index(ShaderProto).InstanceUnique();

        ZIndex = 9; // same as the murk dissolving overlay - over the damage/rainbow overlays

        _config.OnValueChanged(CCVars.ReducedMotion, OnReducedMotionChanged, invokeImmediately: true);
    }

    protected override void DisposeBehavior()
    {
        base.DisposeBehavior();

        _config.UnsubValueChanged(CCVars.ReducedMotion, OnReducedMotionChanged);
        _shader.Dispose();
    }

    private void OnReducedMotionChanged(bool reducedMotion)
    {
        _wind = reducedMotion ? 0f : 1f;
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        var step = args.DeltaSeconds / FadeSeconds;
        _strength = Math.Clamp(_strength + (Active ? step : -step), 0f, 1f);
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        if (args.Viewport.Eye is ScalingViewport.ZEye zEye)
            return zEye.Depth > 0 && zEye.Depth == zEye.HighestDepth;

        if (_playerManager.LocalEntity is not { } player
            || !_entityManager.TryGetComponent(player, out EyeComponent? eyeComp)
            || args.Viewport.Eye != eyeComp.Eye)
            return false;

        // The player's own level is the last pass unless levels above it are rendered too.
        if (!_entityManager.TryGetComponent<CEZLevelViewerComponent>(player, out var viewer) || !viewer.LookUp)
            return true;

        _zLevels ??= _entityManager.System<CEClientZLevelsSystem>();
        return _zLevels.GetVisibleZLevelsAbove(player) == 0;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (ScreenTexture == null)
            return;

        var handle = args.WorldHandle;
        _shader.SetParameter("SCREEN_TEXTURE", ScreenTexture);
        _shader.SetParameter("Strength", _strength);
        _shader.SetParameter("Wind", _wind);
        handle.UseShader(_shader);
        handle.DrawRect(args.WorldBounds, Color.White);
        handle.UseShader(null);
    }
}
