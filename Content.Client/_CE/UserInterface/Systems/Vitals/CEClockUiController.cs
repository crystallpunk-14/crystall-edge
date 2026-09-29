using Content.Client._CE.UserInterface.Screens;
using Content.Client._CE.UserInterface.Systems.Vitals.Widgets;
using Content.Client.UserInterface.Screens;
using Content.Client.UserInterface.Systems.Gameplay;
using Content.Shared._CE.DayCycle;
using JetBrains.Annotations;
using Robust.Client.Player;
using Robust.Client.UserInterface.Controllers;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Client._CE.UserInterface.Systems.Vitals;

/// <summary>
/// Shows the clock widget for players with <see cref="CETimeSenseComponent"/>
/// </summary>
[UsedImplicitly]
public sealed partial class CEClockUiController : UIController
{
    [Dependency] private IPlayerManager _player = default!;

    private CEDayCycleSystem? _dayCycle;
    private CEClockUI? _clock;

    private string? _tooltip;
    private TimeSpan _lastRemaining;
    private bool _lastUntilDawn;

    public override void Initialize()
    {
        base.Initialize();

        var gameplayStateLoad = UIManager.GetUIController<GameplayStateLoadController>();
        gameplayStateLoad.OnScreenLoad += OnScreenLoad;
        gameplayStateLoad.OnScreenUnload += OnScreenUnload;

        SubscribeLocalEvent<LocalPlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<LocalPlayerDetachedEvent>(OnPlayerDetached);
    }

    public override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        UpdateClock();
    }

    private void OnScreenLoad()
    {
        _clock = GetClock();
        UpdateClock();
    }

    private void OnScreenUnload()
    {
        if (_clock != null)
            _clock.Visible = false;

        _clock = null;
    }

    private CEClockUI? GetClock()
    {
        if (UIManager.ActiveScreen is DefaultGameScreen game)
            return game.Clock;

        if (UIManager.ActiveScreen is SeparatedChatGameScreen separated)
            return separated.Clock;

        if (UIManager.ActiveScreen is CEMinimalismGameScreen minimalism)
            return minimalism.Clock;

        return null;
    }

    private void OnPlayerAttached(LocalPlayerAttachedEvent args)
    {
        _clock ??= GetClock();
        UpdateClock();
    }

    private void OnPlayerDetached(LocalPlayerDetachedEvent args)
    {
        if (_clock != null)
            _clock.Visible = false;
    }

    private void UpdateClock()
    {
        if (_clock == null)
            return;

        _dayCycle ??= EntityManager.System<CEDayCycleSystem>();

        if (_player.LocalEntity is not { } player
            || !EntityManager.HasComponent<CETimeSenseComponent>(player)
            || EntityManager.GetComponent<TransformComponent>(player).MapUid is not { } map
            || !_dayCycle.TryGetDayProgress(map, out var progress))
        {
            _clock.Visible = false;
            return;
        }

        // Called every frame - only rebuild the tooltip string when the displayed time changes.
        if (!_dayCycle.TryGetTimeUntilTransition(map, out var remaining, out var untilDawn))
            _tooltip = null;
        else if (_tooltip == null || remaining != _lastRemaining || untilDawn != _lastUntilDawn)
            _dayCycle.TryGetTimeUntilTransitionText(map, out _tooltip);

        _lastRemaining = remaining;
        _lastUntilDawn = untilDawn;

        _clock.Visible = true;
        _clock.SetTime(progress, _tooltip);
    }
}
