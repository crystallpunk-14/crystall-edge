using Content.Client._CE.Achievements.UI;
using JetBrains.Annotations;
using Robust.Client.UserInterface.Controllers;

namespace Content.Client._CE.Achievements;

[UsedImplicitly]
public sealed class CEAchievementsUIController : UIController
{
    private CEAchievementsWindow? _window;

    public void OpenWindow()
    {
        EnsureWindow();

        _window!.OpenCentered();
        _window.MoveToFront();
    }

    private void EnsureWindow()
    {
        if (_window is { Disposed: false })
            return;

        _window = UIManager.CreateWindow<CEAchievementsWindow>();
    }

    public void ToggleWindow()
    {
        EnsureWindow();

        if (_window!.IsOpen)
        {
            _window.Close();
        }
        else
        {
            OpenWindow();
        }
    }
}
