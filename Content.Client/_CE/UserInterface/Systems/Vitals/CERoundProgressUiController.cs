using Content.Client._CE.UserInterface.Screens;
using Content.Client._CE.UserInterface.Systems.Vitals.Widgets;
using Content.Client.Gameplay;
using Content.Client.UserInterface.Screens;
using Content.Client.UserInterface.Systems.Gameplay;
using Content.Shared._CE.Roundflow;
using JetBrains.Annotations;
using Robust.Client.UserInterface.Controllers;
using Robust.Shared.Timing;

namespace Content.Client._CE.UserInterface.Systems.Vitals;

/// <summary>
/// Drives <see cref="CERoundProgressUI"/> from the server's periodic <see cref="CERoundProgressStateEvent"/>,
/// smoothing the displayed values between updates.
/// </summary>
[UsedImplicitly]
public sealed partial class CERoundProgressUiController : UIController, IOnStateExited<GameplayState>
{
    /// <summary>
    /// How fast the displayed values catch up with the last received ones (fraction of the gap per second).
    /// </summary>
    private const float LerpRate = 3f;

    private CERoundProgressUI? _progressBar;

    private bool _visible;
    private float _targetLight;
    private float _targetMurk;
    private float _light;
    private float _murk;

    public override void Initialize()
    {
        base.Initialize();
        var gameplayStateLoad = UIManager.GetUIController<GameplayStateLoadController>();
        gameplayStateLoad.OnScreenLoad += OnScreenLoad;
        gameplayStateLoad.OnScreenUnload += OnScreenUnload;

        SubscribeNetworkEvent<CERoundProgressStateEvent>(OnProgressState);
    }

    public void OnStateExited(GameplayState state)
    {
        _visible = false;
        _targetLight = _targetMurk = _light = _murk = 0f;
    }

    private void OnScreenLoad()
    {
        _progressBar = GetProgressBar();
        UpdateBar();
    }

    private void OnScreenUnload()
    {
        if (_progressBar != null)
            _progressBar.Visible = false;

        _progressBar = null;
    }

    private CERoundProgressUI? GetProgressBar()
    {
        if (UIManager.ActiveScreen is DefaultGameScreen game)
            return game.RoundProgressBar;

        if (UIManager.ActiveScreen is SeparatedChatGameScreen separated)
            return separated.RoundProgressBar;

        if (UIManager.ActiveScreen is CEMinimalismGameScreen minimalism)
            return minimalism.RoundProgressBar;

        return null;
    }

    private void OnProgressState(CERoundProgressStateEvent ev, EntitySessionEventArgs args)
    {
        // Snap instead of sliding in from zero when the bar first appears.
        if (!_visible && ev.Visible)
        {
            _light = ev.Light;
            _murk = ev.Murk;
        }

        _visible = ev.Visible;
        _targetLight = ev.Light;
        _targetMurk = ev.Murk;
        UpdateBar();
    }

    public override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        if (!_visible)
            return;

        var t = Math.Min(1f, args.DeltaSeconds * LerpRate);
        _light += (_targetLight - _light) * t;
        _murk += (_targetMurk - _murk) * t;
        UpdateBar();
    }

    private void UpdateBar()
    {
        if (_progressBar == null)
            return;

        _progressBar.Visible = _visible;
        _progressBar.SetProgress(_light, _murk);
    }
}
