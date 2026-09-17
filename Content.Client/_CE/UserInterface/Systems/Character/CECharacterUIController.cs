using System.Collections.Generic;
using System.Linq;
using Content.Client._CE.Objectives;
using Content.Client._CE.Objectives.Ui;
using Content.Client._CE.Roles;
using Content.Shared._CE.Objectives.Components;
using Content.Client._CE.Skill;
using Content.Client._CE.Skill.Ui;
using Content.Client._CE.UserInterface.Screens;
using Content.Client._CE.UserInterface.Systems.Character.Windows;
using Content.Client.CharacterInfo;
using Content.Client.Gameplay;
using Content.Client.Mind;
using Content.Client.Stylesheets;
using Content.Client.UserInterface.Controls;
using Content.Client.UserInterface.Screens;
using Content.Client.UserInterface.Systems.Inventory;
using Content.Client.UserInterface.Systems.MenuBar.Widgets;
using Content.Shared._CE.Roles;
using Content.Shared.Humanoid;
using Content.Shared.Input;
using Content.Shared.Roles;
using Content.Shared.StatusIcon;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using MenuButton = Content.Client.UserInterface.Controls.MenuButton;
using static Robust.Client.UserInterface.Controls.BaseButton;
using static Content.Client.CharacterInfo.CharacterInfoSystem;

namespace Content.Client._CE.UserInterface.Systems.Character;

[UsedImplicitly]
public sealed partial class CECharacterUIController : UIController, IOnStateEntered<GameplayState>, IOnStateExited<GameplayState>, IOnSystemChanged<CharacterInfoSystem>, IOnSystemChanged<CESecretRoleInfoSystem>, IOnSystemChanged<CEClientSkillSystem>, IOnSystemChanged<CEObjectiveSystem>
{
    private const int ObjectivesTabIndex = 1;
    private const int SkillsTabIndex = 2;

    [UISystemDependency] private readonly CharacterInfoSystem _characterInfo = default!;
    [UISystemDependency] private readonly CESecretRoleInfoSystem _secretRoleInfo = default!;
    [UISystemDependency] private readonly CEClientSkillSystem _clientSkill = default!;
    [UISystemDependency] private readonly CEObjectiveSystem _ceObjectives = default!;
    [UISystemDependency] private readonly MindSystem _mind = default!;
    [UISystemDependency] private readonly SpriteSystem _sprite = default!;
    [Dependency] private IPrototypeManager _prototypeManager = default!;

    private CECharacterWindow? _window;
    private EntityUid? _target;
    private MenuButton? CharacterButton => UIManager.GetActiveUIWidgetOrNull<GameTopMenuBar>()?.CharacterButton;

    private SlotButton? InlineCharacterMenuButton => UIManager.ActiveScreen switch
    {
        DefaultGameScreen game => game.CharacterMenuButton,
        SeparatedChatGameScreen separated => separated.CharacterMenuButton,
        CEMinimalismGameScreen minimalism => minimalism.CharacterMenuButton,
        _ => null
    };

    public void OnSystemLoaded(CharacterInfoSystem system)
    {
        system.OnCharacterUpdate += CharacterUpdated;
    }

    public void OnSystemUnloaded(CharacterInfoSystem system)
    {
        system.OnCharacterUpdate -= CharacterUpdated;
    }

    public void OnSystemLoaded(CESecretRoleInfoSystem system)
    {
        system.OnSecretRoleUpdate += SecretRoleUpdated;
    }

    public void OnSystemUnloaded(CESecretRoleInfoSystem system)
    {
        system.OnSecretRoleUpdate -= SecretRoleUpdated;
    }

    public void OnSystemLoaded(CEClientSkillSystem system)
    {
        system.OnSkillUpdate += SkillsUpdated;
    }

    public void OnSystemUnloaded(CEClientSkillSystem system)
    {
        system.OnSkillUpdate -= SkillsUpdated;
    }

    public void OnSystemLoaded(CEObjectiveSystem system)
    {
        system.OnObjectivesChanged += CEObjectivesChanged;
    }

    public void OnSystemUnloaded(CEObjectiveSystem system)
    {
        system.OnObjectivesChanged -= CEObjectivesChanged;
    }

    private void CEObjectivesChanged(EntityUid holder)
    {
        UpdateCEObjectives();
    }

    // CE objectives are our own networked entities (see CEObjectiveSystem), not part of upstream's
    // CharacterInfoEvent snapshot, so they're populated separately from CharacterUpdated.
    private void UpdateCEObjectives()
    {
        if (_window == null || _target is not { } target)
            return;

        var objectivesTab = _window.ObjectivesTab;
        objectivesTab.CEObjectives.RemoveAllChildren();

        var objectives = _mind.TryGetMind(target, out var mindId, out _)
            ? _ceObjectives.GetObjectives(mindId)
            : new List<Entity<CEObjectiveComponent>>();

        foreach (var objective in objectives)
        {
            var control = new CEObjectiveControl();
            control.SetObjective(objective);
            objectivesTab.CEObjectives.AddChild(control);
        }

        _window.Tabs.SetTabVisible(ObjectivesTabIndex, objectives.Count > 0);
    }

    private void SkillsUpdated(EntityUid entity)
    {
        if (_window == null || entity != _target)
            return;

        UpdateSkillsTab();
    }

    // Clustered by badge (alphabetically by badge name) rather than learn order, so skills from
    // the same source sit together; unbadged skills - most of them, today - sit last, unlabeled.
    private void UpdateSkillsTab()
    {
        if (_window == null || _target is not { } target)
            return;

        var skillsTab = _window.SkillsTab;
        skillsTab.CESkills.RemoveAllChildren();

        var learned = _clientSkill.GetLearnedSkills(target);

        var ordered = learned
            .Select(skill => (skill, descriptor: _clientSkill.GetDescriptor(target, skill)))
            .OrderBy(entry => entry.descriptor is null)
            .ThenBy(entry => entry.descriptor is { } d ? Loc.GetString(d.Name) : string.Empty, StringComparer.CurrentCultureIgnoreCase);

        foreach (var (skill, _) in ordered)
        {
            var control = new CESkillControl();
            control.SetSkill(target, skill);
            skillsTab.CESkills.AddChild(control);
        }

        _window.Tabs.SetTabVisible(SkillsTabIndex, learned.Count > 0);
    }

    private void SecretRoleUpdated(EntityUid entity, ProtoId<CESecretRolePrototype>? secretRoleId)
    {
        if (_window == null)
            return;

        if (secretRoleId is not { } roleId || !_prototypeManager.TryIndex(roleId, out var secretRoleProto))
        {
            _window.SecretRoleContainer.Visible = false;
            return;
        }

        _window.SecretRoleContainer.Visible = true;
        _window.SecretRoleLabel.Text = secretRoleProto.LocalizedName;
        _window.SecretRoleLabel.FontColorOverride = null;
        _window.SecretRoleIcon.Texture = _prototypeManager.TryIndex(secretRoleProto.Icon, out JobIconPrototype? secretRoleIcon)
            ? _sprite.Frame0(secretRoleIcon.Icon)
            : null;

        _window.FactionLabel.Clear();
        foreach (var secretDepartment in _prototypeManager.EnumeratePrototypes<CESecretDepartmentPrototype>())
        {
            if (!secretDepartment.Roles.Contains(roleId))
                continue;

            _window.SecretRoleLabel.FontColorOverride = secretDepartment.Color;

            var factionText = new FormattedMessage();
            factionText.PushColor(secretDepartment.Color);
            factionText.AddText(Loc.GetString(secretDepartment.Name));
            factionText.Pop();
            _window.FactionLabel.SetMessage(factionText);
            break;
        }
    }

    private void CharacterUpdated(CharacterData data)
    {
        if (_window == null)
            return;

        _target = data.Entity;

        var nameText = new FormattedMessage();
        nameText.PushTag(new MarkupNode("bold", null, null));
        nameText.PushTag(new MarkupNode("font", new MarkupParameter("Default"),
            new Dictionary<string, MarkupParameter> { { "size", new MarkupParameter(14L) } }));
        nameText.PushColor(StyleNano.NanoGold);
        nameText.AddText(data.EntityName);
        nameText.Pop();
        nameText.Pop();
        nameText.Pop();
        _window.NameLabel.SetMessage(nameText, tagsAllowed: null);
        _window.SpriteView.SetEntity(data.Entity);

        var detailsText = new FormattedMessage();
        if (EntityManager.TryGetComponent<HumanoidProfileComponent>(data.Entity, out var profile))
        {
            var species = EntityManager.System<HumanoidProfileSystem>().GetSpeciesRepresentation(profile.Species);
            detailsText.AddText($"{profile.Gender} • {profile.Age} • {species}");
        }
        _window.DetailsLabel.SetMessage(detailsText);

        _window.JobLabel.Text = string.Empty;
        _window.JobLabel.FontColorOverride = null;
        _window.JobIcon.Texture = null;
        if (data.JobId is { } jobId && _prototypeManager.TryIndex(jobId, out var jobProto))
        {
            _window.JobLabel.Text = jobProto.LocalizedName;
            _window.JobIcon.Texture = _prototypeManager.TryIndex(jobProto.Icon, out JobIconPrototype? jobIcon)
                ? _sprite.Frame0(jobIcon.Icon)
                : null;

            // A job can be listed under multiple departments - prefer its primary one for the display color.
            DepartmentPrototype? matchedDepartment = null;
            foreach (var department in _prototypeManager.EnumeratePrototypes<DepartmentPrototype>())
            {
                if (!department.Roles.Contains(jobId))
                    continue;

                matchedDepartment = department;
                if (department.Primary)
                    break;
            }

            // Dimmed - the job line reads as secondary to the name/secret role above it.
            _window.JobLabel.FontColorOverride = (matchedDepartment?.Color ?? StyleNano.NanoGold).WithAlpha(0.7f);
        }

        UpdateCEObjectives();

        UpdateSkillsTab();
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
        if (CharacterButton != null)
            CharacterButton.OnPressed -= CharacterButtonPressed;

        if (InlineCharacterMenuButton != null)
            InlineCharacterMenuButton.Pressed -= InlineCharacterMenuButtonPressed;
    }

    public void LoadButton()
    {
        if (CharacterButton != null)
            CharacterButton.OnPressed += CharacterButtonPressed;

        if (InlineCharacterMenuButton != null)
            InlineCharacterMenuButton.Pressed += InlineCharacterMenuButtonPressed;
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

    private void InlineCharacterMenuButtonPressed(GUIBoundKeyEventArgs args, SlotControl control)
    {
        if (args.Function != EngineKeyFunctions.UIClick)
            return;

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
            _secretRoleInfo.RequestSecretRoleInfo();
            UIManager.GetUIController<InventoryUIController>().ReloadSlots();
            _window.OpenToLeft();
        }
    }
}
