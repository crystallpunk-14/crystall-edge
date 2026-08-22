using Content.Client._CE.Skill;
using Content.Client.UserInterface.Controls;
using Content.Shared._CE.Pen;
using Content.Shared._CE.Skill.Components;
using Content.Shared._CE.Skill.Prototypes;
using JetBrains.Annotations;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;

namespace Content.Client._CE.Pen;

[UsedImplicitly]
public sealed partial class CEPenActionsBoundUserInterface : BoundUserInterface
{
    [Dependency] private IPrototypeManager _prototype = default!;
    [Dependency] private IPlayerManager _player = default!;

    private SimpleRadialMenu? _menu;

    public CEPenActionsBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        IoCManager.InjectDependencies(this);
    }

    protected override void Open()
    {
        base.Open();

        if (!EntMan.TryGetComponent<CEPenComponent>(Owner, out var pen) || pen.PendingTarget is not { } target)
            return;

        if (_player.LocalEntity is not { } user)
            return;

        var penSystem = EntMan.System<CEPenSystem>();
        var actions = penSystem.CollectActions(user, Owner, target);

        var models = BuildButtons(actions, user);
        if (models.Count == 0)
            return;

        _menu = this.CreateWindow<SimpleRadialMenu>();
        _menu.Track(Owner);
        _menu.SetButtons(models);
        _menu.OpenOverMouseScreenPosition();
    }

    private List<RadialMenuOptionBase> BuildButtons(List<CEPenAction> actions, EntityUid user)
    {
        // A lone "record skill" action skips its own top-level button - the skill
        // submenu is shown directly, matching what the server does when opening the UI.
        if (actions.Count == 1 && actions[0].Kind == CEPenActionKind.RecordSkill)
            return BuildSkillButtons(user);

        var options = new List<RadialMenuOptionBase>();

        foreach (var action in actions)
        {
            switch (action.Kind)
            {
                case CEPenActionKind.Write:
                    options.Add(new RadialMenuActionOption<CEPenActionKind>(HandleSimpleAction, action.Kind)
                    {
                        ToolTip = Loc.GetString(action.Name),
                        IconSpecifier = RadialMenuIconSpecifier.With(action.Icon),
                    });
                    break;

                case CEPenActionKind.RecordSkill:
                    var nested = BuildSkillButtons(user);
                    if (nested.Count == 0)
                        break;

                    options.Add(new RadialMenuNestedLayerOption(nested)
                    {
                        ToolTip = Loc.GetString(action.Name),
                        IconSpecifier = RadialMenuIconSpecifier.With(action.Icon),
                    });
                    break;
            }
        }

        return options;
    }

    private List<RadialMenuOptionBase> BuildSkillButtons(EntityUid user)
    {
        var options = new List<RadialMenuOptionBase>();

        if (!EntMan.TryGetComponent<CESkillStorageComponent>(user, out var storage))
            return options;

        var skillSystem = EntMan.System<CEClientSkillSystem>();

        foreach (var skillId in storage.LearnedSkills)
        {
            if (!_prototype.TryIndex(skillId, out var skill))
                continue;

            var texture = skillSystem.GetSkillIcon(skillId);

            options.Add(new RadialMenuActionOption<ProtoId<CESkillPrototype>>(HandleSkillPicked, skillId)
            {
                ToolTip = skillSystem.GetSkillName(skillId),
                IconSpecifier = texture is not null
                    ? RadialMenuIconSpecifier.With(texture)
                    : RadialMenuIconSpecifier.With(skill.PreviewEntity),
            });
        }

        return options;
    }

    private void HandleSimpleAction(CEPenActionKind kind)
    {
        SendMessage(new CEPenActionsMessage(kind));
    }

    private void HandleSkillPicked(ProtoId<CESkillPrototype> skill)
    {
        SendMessage(new CEPenActionsMessage(CEPenActionKind.RecordSkill, skill));
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            _menu?.Close();
            _menu = null;
        }
    }
}
