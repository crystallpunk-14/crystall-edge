using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Content.Client._CE.Objectives;
using Content.Client._CE.Objectives.Ui;
using Content.Client._CE.Roles;
using Content.Shared._CE.Objectives.Components;
using Content.Client._CE.Skill;
using Content.Client._CE.SkillTree.Ui;
using Content.Client._CE.UserInterface.Screens;
using Content.Client._CE.UserInterface.Systems.Character.Windows;
using Content.Client._CE.UserInterface.Systems.NodeTree;
using Content.Client.CharacterInfo;
using Content.Client.Gameplay;
using Content.Client.Mind;
using Content.Client.Stylesheets;
using Content.Client.UserInterface.Controls;
using Content.Client.UserInterface.Screens;
using Content.Client.UserInterface.Systems.Inventory;
using Content.Client.UserInterface.Systems.MenuBar.Widgets;
using Content.Shared._CE.EntityEffect;
using Content.Shared._CE.Roles;
using Content.Shared._CE.Skill.Prototypes;
using Content.Shared._CE.SkillTree;
using Content.Shared._CE.SkillTree.Prototypes;
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
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using MenuButton = Content.Client.UserInterface.Controls.MenuButton;
using static Robust.Client.UserInterface.Controls.BaseButton;
using static Content.Client.CharacterInfo.CharacterInfoSystem;

namespace Content.Client._CE.UserInterface.Systems.Character;

[UsedImplicitly]
public sealed partial class CECharacterUIController : UIController, IOnStateEntered<GameplayState>, IOnStateExited<GameplayState>, IOnSystemChanged<CharacterInfoSystem>, IOnSystemChanged<CESecretRoleInfoSystem>, IOnSystemChanged<CESkillTreeSystem>, IOnSystemChanged<CEObjectiveSystem>
{
    private const int ObjectivesTabIndex = 1;
    private const int SkillsTabIndex = 2;

    /// <summary>
    /// Spacing, in pixels, between adjacent grid steps of <see cref="CESkillTreeNode.Position"/>.
    /// </summary>
    private const float SkillTreeNodeSpacing = 25f;

    [UISystemDependency] private readonly CharacterInfoSystem _characterInfo = default!;
    [UISystemDependency] private readonly CESecretRoleInfoSystem _secretRoleInfo = default!;
    [UISystemDependency] private readonly CESkillTreeSystem _skillTreeSystem = default!;
    [UISystemDependency] private readonly CEClientSkillSystem _clientSkill = default!;
    [UISystemDependency] private readonly CEObjectiveSystem _ceObjectives = default!;
    [UISystemDependency] private readonly MindSystem _mind = default!;
    [UISystemDependency] private readonly SpriteSystem _sprite = default!;
    [Dependency] private IPrototypeManager _prototypeManager = default!;

    private CECharacterWindow? _window;
    private CESkillNodeTooltip? _skillTooltip;
    private EntityUid? _target;
    private ProtoId<CESkillTreePrototype>? _selectedTree;
    private ProtoId<CESkillPrototype>? _selectedSkill;
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

    public void OnSystemLoaded(CESkillTreeSystem system)
    {
        system.OnSkillTreeUpdate += SkillTreeUpdated;
    }

    public void OnSystemUnloaded(CESkillTreeSystem system)
    {
        system.OnSkillTreeUpdate -= SkillTreeUpdated;
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

    private void SkillTreeUpdated(EntityUid entity)
    {
        if (_window == null || entity != _target)
            return;

        UpdateSkillsTab();
    }

    private void UpdateSkillsTab()
    {
        if (_window == null || _target is not { } target)
            return;

        var trees = _skillTreeSystem.GetAvailableTrees(target);

        _window.Tabs.SetTabVisible(SkillsTabIndex, trees.Count > 0);

        if (trees.Count == 0)
            return;

        if (_selectedTree is not { } selected || !trees.Contains(selected))
            selected = trees.First();

        SelectTree(target, selected, trees);
    }

    private void SelectTree(EntityUid target,
        ProtoId<CESkillTreePrototype> tree,
        IReadOnlySet<ProtoId<CESkillTreePrototype>> availableTrees)
    {
        if (_window == null || !_prototypeManager.TryIndex(tree, out var treeProto))
            return;

        _selectedTree = tree;

        var skillsTab = _window.SkillsTab;
        skillsTab.TreeName.Text = Loc.GetString(treeProto.Name);
        skillsTab.ParallaxBackground.ParallaxPrototype = treeProto.Parallax;

        skillsTab.TreeTabsContainer.RemoveAllChildren();
        foreach (var otherTree in availableTrees)
        {
            if (!_prototypeManager.TryIndex(otherTree, out var otherTreeProto))
                continue;

            var otherTreePoints = _skillTreeSystem.GetPoints(target, otherTree);
            var otherTreeIcon = otherTreeProto.PointsIcon is { } otherIcon ? _sprite.Frame0(otherIcon) : null;

            var button = new CESkillTreeButtonControl(otherTreeProto.Color, Loc.GetString(otherTreeProto.Name), otherTreePoints, otherTreeIcon)
            {
                ToolTip = otherTreeProto.Desc is { } desc ? Loc.GetString(desc) : string.Empty,
            };
            button.OnPressed += () => SelectTree(target, otherTree, availableTrees);
            skillsTab.TreeTabsContainer.AddChild(button);
        }

        var points = _skillTreeSystem.GetPoints(target, tree);
        skillsTab.PointsLabel.Text = points.ToString(CultureInfo.InvariantCulture);
        skillsTab.PointsIcon.Texture = treeProto.PointsIcon is { } pointsIcon ? _sprite.Frame0(pointsIcon) : null;

        var nodes = new HashSet<CENodeTreeElement>();
        var edges = new HashSet<(string, string)>();

        CESkillTreeNode? selectedNode = null;
        var selectedNodeActive = false;

        void Walk(CESkillTreeNode node, string? parentKey, bool parentLearned)
        {
            var learned = _clientSkill.HaveSkill(target, node.Skill);
            var active = !learned && parentLearned && points >= node.Cost && _clientSkill.SkillConditionsMet(target, node.Skill);

            nodes.Add(new CENodeTreeElement(node.Skill.Id, learned, active, node.Position * SkillTreeNodeSpacing, _clientSkill.GetSkillIcon(node.Skill)));

            if (parentKey != null)
                edges.Add((parentKey, node.Skill.Id));

            if (_selectedSkill is { } selectedSkill && node.Skill == selectedSkill)
            {
                selectedNode = node;
                selectedNodeActive = active;
            }

            foreach (var child in node.Children)
                Walk(child, node.Skill.Id, learned);
        }

        foreach (var root in treeProto.Nodes)
            Walk(root, null, true);

        skillsTab.GraphControl.UpdateState(new CENodeTreeUiState(
            nodes,
            edges,
            treeProto.FrameIcon,
            treeProto.HoveredIcon,
            treeProto.SelectedIcon,
            treeProto.LearnedIcon));

        UpdateLearnButton(target, tree, treeProto, selectedNode, selectedNodeActive);
    }

    private void UpdateLearnButton(EntityUid target,
        ProtoId<CESkillTreePrototype> tree,
        CESkillTreePrototype treeProto,
        CESkillTreeNode? selectedNode,
        bool canLearn)
    {
        if (_window == null)
            return;

        var learnButtonContainer = _window.SkillsTab.LearnButtonContainer;
        learnButtonContainer.RemoveAllChildren();

        learnButtonContainer.Visible = selectedNode != null && canLearn;
        if (selectedNode == null || !canLearn)
            return;

        var skill = selectedNode.Skill;
        var icon = treeProto.PointsIcon is { } pointsIcon ? _sprite.Frame0(pointsIcon) : null;
        var learnButton = new CESkillTreeButtonControl(treeProto.Color, Loc.GetString("ce-skill-tree-learn-button"), selectedNode.Cost, icon);
        learnButton.OnPressed += () => _skillTreeSystem.RequestLearnSkillTreeNode(target, tree, skill);
        learnButtonContainer.AddChild(learnButton);
    }

    private void OnSkillNodeSelected(CENodeTreeElement? node)
    {
        if (node != null)
            _selectedSkill = new ProtoId<CESkillPrototype>(node.NodeKey);
        else
            _selectedSkill = null;

        if (_target is not { } target || _selectedTree is not { } tree)
            return;

        SelectTree(target, tree, _skillTreeSystem.GetAvailableTrees(target));
    }

    private static CESkillTreeNode? FindTreeNode(IEnumerable<CESkillTreeNode> nodes, ProtoId<CESkillPrototype> skill)
    {
        foreach (var node in nodes)
        {
            if (node.Skill == skill)
                return node;

            if (FindTreeNode(node.Children, skill) is { } found)
                return found;
        }

        return null;
    }

    private void OnSkillNodeHovered(CENodeTreeElement? node)
    {
        HideSkillTooltip();

        if (node == null || _target is not { } target)
            return;

        var skillId = new ProtoId<CESkillPrototype>(node.NodeKey);
        if (!_prototypeManager.TryIndex(skillId, out var skillProto))
            return;

        _skillTooltip = new CESkillNodeTooltip();
        _skillTooltip.NameLabel.Text = _clientSkill.GetSkillName(skillId);

        var descriptionText = new FormattedMessage();
        var flavor = _clientSkill.GetSkillDescription(skillId);
        if (!string.IsNullOrEmpty(flavor))
            descriptionText.AddText(flavor);

        var effect = _clientSkill.GetSkillEffectDescription(skillId);
        if (!string.IsNullOrEmpty(effect))
        {
            if (descriptionText.Nodes.Count > 0)
                descriptionText.PushNewline();
            descriptionText.AddText(effect);
        }

        _skillTooltip.DescriptionLabel.Visible = descriptionText.Nodes.Count > 0;
        _skillTooltip.DescriptionLabel.SetMessage(descriptionText);

        _skillTooltip.RequirementsContainer.RemoveAllChildren();
        var args = new CEEntityEffectArgs(EntityManager, target, null, Angle.Zero, 0f, target, null);
        foreach (var condition in skillProto.Conditions)
        {
            var conditionDesc = condition.GetDescription(EntityManager, _prototypeManager);
            if (string.IsNullOrEmpty(conditionDesc))
                continue;

            var requirementText = new FormattedMessage();
            requirementText.PushColor(condition.Passes(args) ? Color.LimeGreen : Color.Red);
            requirementText.AddText($"- {conditionDesc}");
            requirementText.Pop();

            var requirementLabel = new RichTextLabel();
            requirementLabel.SetMessage(requirementText);
            _skillTooltip.RequirementsContainer.AddChild(requirementLabel);
        }

        CESkillTreeNode? treeNode = null;
        CESkillTreePrototype? treeProto = null;
        if (_selectedTree is { } selectedTree && _prototypeManager.TryIndex(selectedTree, out treeProto))
            treeNode = FindTreeNode(treeProto.Nodes, skillId);

        _skillTooltip.CostContainer.Visible = treeNode != null;
        if (treeNode != null)
        {
            _skillTooltip.CostLabel.Text = treeNode.Cost.ToString(CultureInfo.InvariantCulture);
            _skillTooltip.CostIcon.Texture = treeProto?.PointsIcon is { } costIcon ? _sprite.Frame0(costIcon) : null;
        }

        UIManager.PopupRoot.AddChild(_skillTooltip);
        Tooltips.PositionTooltip(_skillTooltip);
    }

    private void HideSkillTooltip()
    {
        if (_skillTooltip == null)
            return;

        UIManager.PopupRoot.RemoveChild(_skillTooltip);
        _skillTooltip = null;
    }

    public override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        if (_skillTooltip != null)
            Tooltips.PositionTooltip(_skillTooltip);
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
        _window.SkillsTab.GraphControl.OnNodeHovered += OnSkillNodeHovered;
        _window.SkillsTab.GraphControl.OnNodeSelected += OnSkillNodeSelected;

        CommandBinds.Builder
            .Bind(ContentKeyFunctions.OpenCharacterMenu,
                InputCmdHandler.FromDelegate(_ => ToggleWindow()))
            .Register<CECharacterUIController>();
    }

    public void OnStateExited(GameplayState state)
    {
        HideSkillTooltip();

        if (_window != null)
        {
            UIManager.GetUIController<InventoryUIController>().RemoveSlotGroup(_window.InventoryTab.InventorySlots.SlotGroup);

            _window.SkillsTab.GraphControl.OnNodeHovered -= OnSkillNodeHovered;
            _window.SkillsTab.GraphControl.OnNodeSelected -= OnSkillNodeSelected;
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
