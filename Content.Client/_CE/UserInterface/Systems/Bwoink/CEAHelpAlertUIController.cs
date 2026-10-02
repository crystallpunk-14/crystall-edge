using Content.Client._CE.UserInterface.Screens;
using Content.Client.Stylesheets;
using Content.Client.UserInterface.Controls;
using Content.Client.UserInterface.Systems.Bwoink;
using Content.Client.UserInterface.Systems.Gameplay;
using JetBrains.Annotations;
using Robust.Client.UserInterface.Controllers;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Timing;

namespace Content.Client._CE.UserInterface.Systems.Bwoink;

/// <summary>
/// The minimalist HUD hides the top menu bar, and with it the AHelp button that turns red on an
/// unread AHelp message. While the top bar is hidden and such a message is waiting, this shows a
/// standalone AHelp button in its place, blinking between the red and the normal style.
/// </summary>
[UsedImplicitly]
public sealed partial class CEAHelpAlertUIController : UIController
{
    [Dependency] private IGameTiming _timing = default!;

    // Seconds between switching the red style on and off.
    private const double BlinkIntervalSeconds = 1.0;

    private MenuButton? _button;

    public override void Initialize()
    {
        base.Initialize();

        var gameplayStateLoad = UIManager.GetUIController<GameplayStateLoadController>();
        gameplayStateLoad.OnScreenLoad += OnScreenLoad;
        gameplayStateLoad.OnScreenUnload += OnScreenUnload;
    }

    private void OnScreenLoad()
    {
        if (UIManager.ActiveScreen is not CEMinimalismGameScreen minimalism)
            return;

        _button = minimalism.AHelpAlertButton;
        _button.OnPressed += OnButtonPressed;
        UpdateButton();
    }

    private void OnScreenUnload()
    {
        if (_button == null)
            return;

        _button.OnPressed -= OnButtonPressed;
        _button = null;
    }

    private void OnButtonPressed(BaseButton.ButtonEventArgs args)
    {
        // Opening the window marks the AHelp as read, which hides this button again.
        UIManager.GetUIController<AHelpUIController>().ToggleWindow();
    }

    public override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        UpdateButton();
    }

    private void UpdateButton()
    {
        if (_button == null || UIManager.ActiveScreen is not CEMinimalismGameScreen minimalism)
            return;

        var show = !minimalism.TopBar.Visible && UIManager.GetUIController<AHelpUIController>().HasUnreadAHelp;
        _button.Visible = show;

        var red = show && (long) (_timing.RealTime.TotalSeconds / BlinkIntervalSeconds) % 2 == 0;
        if (red == _button.HasStyleClass(StyleClass.Negative))
            return;

        if (red)
            _button.AddStyleClass(StyleClass.Negative);
        else
            _button.RemoveStyleClass(StyleClass.Negative);
    }
}
