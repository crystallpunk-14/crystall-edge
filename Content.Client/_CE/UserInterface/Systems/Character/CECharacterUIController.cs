using System.Linq;
using Content.Client._CE.UserInterface.Systems.Character.Windows;
using Content.Client.CharacterInfo;
using Content.Client.Gameplay;
using Content.Client.Stylesheets;
using Content.Client.UserInterface.Systems.Character.Controls;
using Content.Client.UserInterface.Systems.Inventory;
using Content.Client.UserInterface.Systems.MenuBar.Widgets;
using Content.Client.UserInterface.Systems.Objectives.Controls;
using Content.Shared.Humanoid;
using Content.Shared.Input;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input.Binding;
using Robust.Shared.Utility;
using MenuButton = Content.Client.UserInterface.Controls.MenuButton;
using static Robust.Client.UserInterface.Controls.BaseButton;
using static Content.Client.CharacterInfo.CharacterInfoSystem;

namespace Content.Client._CE.UserInterface.Systems.Character;

[UsedImplicitly]
public sealed partial class CECharacterUIController : UIController, IOnStateEntered<GameplayState>, IOnStateExited<GameplayState>, IOnSystemChanged<CharacterInfoSystem>
{
    [UISystemDependency] private readonly CharacterInfoSystem _characterInfo = default!;
    [UISystemDependency] private readonly SpriteSystem _sprite = default!;

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

        _window.InventoryTab.DetailsLabel.Text = EntityManager.TryGetComponent<HumanoidProfileComponent>(data.Entity, out var profile)
            ? $"{EntityManager.System<HumanoidProfileSystem>().GetSpeciesRepresentation(profile.Species)}, {profile.Age}, {profile.Gender}"
            : string.Empty;

        var objectivesTab = _window.ObjectivesTab;
        objectivesTab.Objectives.RemoveAllChildren();
        objectivesTab.ObjectivesLabel.Visible = data.Objectives.Any();

        foreach (var (groupId, conditions) in data.Objectives)
        {
            var objectiveControl = new CharacterObjectiveControl
            {
                Orientation = BoxContainer.LayoutOrientation.Vertical,
                Modulate = Color.Gray
            };

            var objectiveText = new FormattedMessage();
            objectiveText.TryAddMarkup(groupId, out _);

            var objectiveLabel = new RichTextLabel
            {
                StyleClasses = { StyleClass.TooltipTitle }
            };
            objectiveLabel.SetMessage(objectiveText);

            objectiveControl.AddChild(objectiveLabel);

            foreach (var condition in conditions)
            {
                var conditionControl = new ObjectiveConditionsControl();
                conditionControl.ProgressTexture.Texture = _sprite.Frame0(condition.Icon);
                conditionControl.ProgressTexture.Progress = condition.Progress;
                var titleMessage = new FormattedMessage();
                var descriptionMessage = new FormattedMessage();
                titleMessage.AddText(condition.Title);
                descriptionMessage.AddText(condition.Description);

                conditionControl.Title.SetMessage(titleMessage);
                conditionControl.Description.SetMessage(descriptionMessage);

                objectiveControl.AddChild(conditionControl);
            }

            objectivesTab.Objectives.AddChild(objectiveControl);
        }

        if (data.Briefing != null)
        {
            var briefingControl = new ObjectiveBriefingControl();
            var text = new FormattedMessage();
            text.PushColor(Color.Yellow);
            text.AddText(data.Briefing);
            briefingControl.Label.SetMessage(text);
            objectivesTab.Objectives.AddChild(briefingControl);
        }

        var controls = _characterInfo.GetCharacterInfoControls(data.Entity);
        foreach (var control in controls)
        {
            objectivesTab.Objectives.AddChild(control);
        }

        objectivesTab.RolePlaceholder.Visible = data.Briefing == null && !controls.Any() && !data.Objectives.Any();
    }

    public void OnStateEntered(GameplayState state)
    {
        DebugTools.Assert(_window == null);

        _window = UIManager.CreateWindow<CECharacterWindow>();

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
            UIManager.GetUIController<InventoryUIController>().RemoveSlotGroup(_window.InventoryTab.InventorySlots.SlotGroup);

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
            _window.OpenToLeft();
        }
    }
}
