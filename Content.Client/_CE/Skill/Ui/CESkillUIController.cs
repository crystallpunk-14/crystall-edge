using System.Linq;
using System.Numerics;
using System.Text;
using Content.Client._CE.Skill.Ui.Window;
using Content.Client._CE.UserInterface.Systems.NodeTree;
using Content.Client.Gameplay;
using Content.Client.UserInterface.Controls;
using Content.Shared._CE.Skill.Components;
using Content.Shared._CE.Skill.Prototypes;
using Content.Shared._CE.Skill.Restrictions;
using Content.Shared._CE.Input;
using JetBrains.Annotations;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using Robust.Client.UserInterface.Controls;
using Robust.Client.Utility;
using Robust.Shared.Input.Binding;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client._CE.Skill.Ui;

[UsedImplicitly]
public sealed class CESkillUIController : UIController, IOnStateEntered<GameplayState>, IOnStateExited<GameplayState>,
    IOnSystemChanged<CEClientSkillSystem>
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IEntityManager _entManager = default!;
    [UISystemDependency] private readonly CEClientSkillSystem _skill = default!;

    private CESkillWindow? _window;
    private EntityUid? _targetPlayer;

    private IEnumerable<CESkillPrototype> _allSkills = [];

    private CESkillPrototype? _selectedSkill;
    private CESkillTreePrototype? _selectedSkillTree;

    private MenuButton? SkillButton => UIManager
        .GetActiveUIWidgetOrNull<Client.UserInterface.Systems.MenuBar.Widgets.GameTopMenuBar>()
        ?.CESkillButton;

    public void OnStateEntered(GameplayState state)
    {
        DebugTools.Assert(_window == null);

        _window = UIManager.CreateWindow<CESkillWindow>();
        LayoutContainer.SetAnchorPreset(_window, LayoutContainer.LayoutPreset.CenterTop);

        CommandBinds.Builder
            .Bind(CEContentKeyFunctions.OpenSkillMenu,
                InputCmdHandler.FromDelegate(_ => ToggleWindow()))
            .Register<CESkillUIController>();

        CacheSkillProto();
        _proto.PrototypesReloaded += _ => CacheSkillProto();

        _window.LearnButton.OnPressed += _ => _skill.RequestLearnSkill(_playerManager.LocalEntity, _selectedSkill);
        _window.GraphControl.OnNodeSelected += SelectNode;
        _window.GraphControl.OnOffsetChanged += offset =>
        {
            _window.ParallaxBackground.Offset = -offset * 0.25f + new Vector2(1000, 1000); //hardcoding is bad
        };
    }

    private void CacheSkillProto()
    {
        _allSkills = _proto.EnumeratePrototypes<CESkillPrototype>();
    }

    public void OnStateExited(GameplayState state)
    {
        if (_window != null)
        {
            _window.GraphControl.OnNodeSelected -= SelectNode;

            _window.Dispose();
            _window = null;
        }

        CommandBinds.Unregister<CESkillUIController>();
    }

    public void OnSystemLoaded(CEClientSkillSystem system)
    {
        system.OnSkillUpdate += UpdateState;
        _playerManager.LocalPlayerDetached += CharacterDetached;
    }

    public void OnSystemUnloaded(CEClientSkillSystem system)
    {
        system.OnSkillUpdate -= UpdateState;
        _playerManager.LocalPlayerDetached -= CharacterDetached;
    }

    public void UnloadButton()
    {
        if (SkillButton is null)
            return;

        SkillButton.OnPressed -= SkillButtonPressed;
    }

    public void LoadButton()
    {
        if (SkillButton is null)
            return;

        SkillButton.OnPressed += SkillButtonPressed;

        if (_window is null)
            return;

        _window.OnClose += DeactivateButton;
        _window.OnOpen += ActivateButton;
    }

    private void DeactivateButton()
    {
        SkillButton!.Pressed = false;
    }

    private void ActivateButton()
    {
        SkillButton!.Pressed = true;
    }

    private void SelectNode(CENodeTreeElement? node)
    {
        if (_window is null)
            return;

        if (_targetPlayer == null)
            return;

        if (node == null)
        {
            DeselectNode();
            return;
        }

        if (!_proto.Resolve<CESkillPrototype>(node.NodeKey, out var skill))
        {
            DeselectNode();
            return;
        }

        SelectNode(skill);
    }

    private void SelectNode(CESkillPrototype? skill)
    {
        if (skill is null)
        {
            DeselectNode();
            UpdateGraphControl();
            return;
        }

        if (_window is null)
            return;

        if (_targetPlayer == null)
            return;

        if (!_proto.Resolve(skill.Tree, out var indexedTree))
            return;

        if (!_proto.Resolve(indexedTree.SkillType, out var indexedSkillType))
            return;

        _selectedSkill = skill;

        _window.SkillName.Text = _skill.GetSkillName(skill);
        _window.SkillDescription.SetMessage(GetSkillDescription(skill));
        _window.SkillFree.Visible = _skill.HaveFreeSkill(_targetPlayer.Value, skill);
        _window.SkillView.Texture = skill.Icon.Frame0();
        _window.LearnButton.Disabled = !_skill.CanLearnSkill(_targetPlayer.Value, skill);
        _window.SkillPointText.Text =
            Loc.GetString("ce-skill-menu-learncost", ("type", Loc.GetString(indexedSkillType.Name)));
        _window.SkillCost.Text = skill.LearnCost.ToString();
        _window.SkillPointIcon.Texture = indexedSkillType.Icon?.Frame0();

        UpdateGraphControl();
    }

    private void DeselectNode()
    {
        if (_window is null)
            return;

        _window.SkillName.Text = string.Empty;
        _window.SkillDescription.Text = string.Empty;
        _window.SkillFree.Visible = false;
        _window.SkillView.Texture = null;
        _window.LearnButton.Disabled = true;
    }

    private FormattedMessage GetSkillDescription(CESkillPrototype skill)
    {
        var msg = new FormattedMessage();

        if (_targetPlayer == null)
            return msg;

        var sb = new StringBuilder();

        //Description
        sb.Append(_skill.GetSkillDescription(skill) + "\n \n");

        if (!_skill.HaveSkill(_targetPlayer.Value, skill))
        {
            //Restrictions
            foreach (var req in skill.Restrictions)
            {
                var color = req.Check(_entManager, _targetPlayer.Value) ? "green" : "red";

                sb.Append($"- [color={color}]{req.GetDescription(_entManager, _proto)}[/color]\n");
            }
        }

        msg.TryAddMarkup(sb.ToString(), out _);

        return msg;
    }

    private void UpdateGraphControl()
    {
        if (_window is null)
            return;

        if (_selectedSkillTree == null)
            return;

        if (!EntityManager.TryGetComponent<CESkillStorageComponent>(_targetPlayer, out var storage))
            return;

        if (!_proto.Resolve(_selectedSkillTree.SkillType, out var indexedSkillType))
            return;

        var skillPointsMap = storage.SkillPoints;

        _window.LevelLabel.Text = skillPointsMap.TryGetValue(_selectedSkillTree.SkillType, out var skillContainer)
            ? $"{Loc.GetString(indexedSkillType.Name)}: {skillContainer.Sum}/{skillContainer.Max}"
            : $"{Loc.GetString(indexedSkillType.Name)}: 0/0";

        _window.LevelTexture.Texture = indexedSkillType.Icon?.Frame0();

        HashSet<CENodeTreeElement> nodeTreeElements = new();

        HashSet<(string, string)> nodeTreeEdges = new();

        var learned = storage.LearnedSkills;
        foreach (var skill in _allSkills)
        {
            if (skill.Tree != _selectedSkillTree)
                continue;

            var hide = false;
            foreach (var req in skill.Restrictions)
            {
                if (req.HideFromUI && !req.Check(_entManager, _targetPlayer.Value))
                {
                    hide = true;
                    break;
                }

                switch (req)
                {
                    case NeedPrerequisite prerequisite:
                        if (!_proto.Resolve(prerequisite.Prerequisite, out var prerequisiteSkill))
                            continue;

                        if (prerequisiteSkill.Tree != _selectedSkillTree)
                            continue;

                        nodeTreeEdges.Add((skill.ID, prerequisiteSkill.ID));
                        break;
                }
            }

            if (!hide)
            {
                var nodeTreeElement = new CENodeTreeElement(
                    skill.ID,
                    gained: learned.Contains(skill),
                    active: _skill.CanLearnSkill(_targetPlayer.Value, skill),
                    skill.SkillUiPosition * 25f,
                    skill.Icon);
                nodeTreeElements.Add(nodeTreeElement);
            }
        }

        _window.GraphControl.UpdateState(
            new CENodeTreeUiState(
                nodes: nodeTreeElements,
                edges: nodeTreeEdges,
                frameIcon: _selectedSkillTree.FrameIcon,
                hoveredIcon: _selectedSkillTree.HoveredIcon,
                selectedIcon: _selectedSkillTree.SelectedIcon,
                learnedIcon: _selectedSkillTree.LearnedIcon
            )
        );
    }

    private void UpdateState(EntityUid player)
    {
        _targetPlayer = player;

        if (_window is null)
            return;

        if (!EntityManager.TryGetComponent<CESkillStorageComponent>(_targetPlayer, out var storage))
            return;

        //If tree not selected, select the first one
        if (_selectedSkillTree == null)
        {
            var firstTree = storage.AvailableSkillTrees.First();

            SelectTree(firstTree); // Set the first tree from the player's progress
        }

        if (_selectedSkillTree == null)
            return;

        // Reselect for update state
        SelectNode(_selectedSkill);
        UpdateGraphControl();

        _window.TreeTabsContainer.RemoveAllChildren();
        foreach (var tree in storage.AvailableSkillTrees)
        {
            if (!_proto.Resolve(tree, out var indexedTree))
                return;

            if (!_proto.Resolve(indexedTree.SkillType, out var indexedSkillType))
                return;

            float learnedPoints = 0;
            foreach (var skillId in storage.LearnedSkills)
            {
                //TODO: Loop indexing each skill is bad
                if (_proto.Resolve(skillId, out var skill) && skill.Tree == tree)
                {
                    if (_skill.HaveFreeSkill(_targetPlayer.Value, skillId))
                        continue;
                    learnedPoints += skill.LearnCost;
                }
            }

            var treeButton = new CESkillTreeButtonControl(indexedTree.Color,
                Loc.GetString(indexedTree.Name),
                learnedPoints,
                indexedSkillType.Icon?.Frame0());
            treeButton.ToolTip = Loc.GetString(indexedTree.Desc ?? string.Empty);
            treeButton.OnPressed += () =>
            {
                SelectTree(indexedTree);
            };

            _window.TreeTabsContainer.AddChild(treeButton);
        }
    }

    private void SelectTree(ProtoId<CESkillTreePrototype> tree)
    {
        if (_window == null)
            return;

        if (!_proto.Resolve(tree, out var indexedTree))
            return;

        _selectedSkillTree = indexedTree;
        _window.ParallaxBackground.ParallaxPrototype = indexedTree.Parallax;
        _window.TreeName.Text = Loc.GetString(indexedTree.Name);

        UpdateGraphControl();
    }

    private void CharacterDetached(EntityUid uid)
    {
        CloseWindow();
    }

    private void SkillButtonPressed(BaseButton.ButtonEventArgs args)
    {
        ToggleWindow();
    }

    private void CloseWindow()
    {
        _window?.Close();
    }

    private void ToggleWindow()
    {
        if (_window == null)
            return;

        if (SkillButton != null)
        {
            SkillButton.SetClickPressed(!_window.IsOpen);
        }

        if (_window.IsOpen)
        {
            CloseWindow();
        }
        else
        {
            _skill.RequestSkillData();
            _window.Open();
        }
    }
}
