using Content.Client._CE.UserInterface.Systems.Character.Windows;
using Content.Client.CharacterInfo;
using Content.Client.Gameplay;
using Content.Client.UserInterface.Systems.Inventory;
using Content.Client.UserInterface.Systems.MenuBar.Widgets;
using Content.Shared.Input;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input.Binding;
using Robust.Shared.Utility;
using MenuButton = Content.Client.UserInterface.Controls.MenuButton;
using static Robust.Client.UserInterface.Controls.BaseButton;
using static Content.Client.CharacterInfo.CharacterInfoSystem;

namespace Content.Client._CE.UserInterface.Systems.Character;

/// <summary>
/// Replaces the standard character menu on the OpenCharacterMenu hotkey/button with our own
/// window, currently showing an inventory tab, while more tabs are developed.
/// </summary>
[UsedImplicitly]
public sealed partial class CECharacterUIController : UIController, IOnStateEntered<GameplayState>, IOnStateExited<GameplayState>, IOnSystemChanged<CharacterInfoSystem>
{
    [UISystemDependency] private readonly CharacterInfoSystem _characterInfo = default!;

    private CECharacterWindow? _window;
    private MenuButton? CharacterButton => UIManager.GetActiveUIWidgetOrNull<GameTopMenuBar>()?.CharacterButton;

    public void OnSystemLoaded(CharacterInfoSystem system)
    {
        system.OnCharacterUpdate += CharacterUpdated;
    }

    public void OnSystemUnloaded(CharacterInfoSystem system)
    {
        system.OnCharacterUpdate -= CharacterUpdated;
    }

    private void CharacterUpdated(CharacterData data)
    {
        if (_window == null)
            return;

        _window.InventoryTab.NameLabel.Text = data.EntityName;
        _window.InventoryTab.SpriteView.SetEntity(data.Entity);
    }

    public void OnStateEntered(GameplayState state)
    {
        DebugTools.Assert(_window == null);

        _window = UIManager.CreateWindow<CECharacterWindow>();
        LayoutContainer.SetAnchorPreset(_window, LayoutContainer.LayoutPreset.CenterTop);

        _window.OnClose += DeactivateButton;
        _window.OnOpen += ActivateButton;

        CommandBinds.Builder
            .Bind(ContentKeyFunctions.OpenCharacterMenu,
                InputCmdHandler.FromDelegate(_ => ToggleWindow()))
            .Register<CECharacterUIController>();
    }

    public void OnStateExited(GameplayState state)
    {
        if (_window != null)
        {
            _window.Close();
            _window = null;
        }

        CommandBinds.Unregister<CECharacterUIController>();
    }

    public void UnloadButton()
    {
        if (CharacterButton == null)
            return;

        CharacterButton.OnPressed -= CharacterButtonPressed;
    }

    public void LoadButton()
    {
        if (CharacterButton == null)
            return;

        CharacterButton.OnPressed += CharacterButtonPressed;
    }

    private void DeactivateButton()
    {
        if (CharacterButton == null)
            return;

        CharacterButton.Pressed = false;
    }

    private void ActivateButton()
    {
        if (CharacterButton == null)
            return;

        CharacterButton.Pressed = true;
    }

    private void CharacterButtonPressed(ButtonEventArgs args)
    {
        ToggleWindow();
    }

    private void ToggleWindow()
    {
        if (_window == null)
            return;

        CharacterButton?.SetClickPressed(!_window.IsOpen);

        if (_window.IsOpen)
        {
            _window.Close();
        }
        else
        {
            _characterInfo.RequestCharacterInfo();
            UIManager.GetUIController<InventoryUIController>().ReloadSlots();
            _window.Open();
        }
    }
}
