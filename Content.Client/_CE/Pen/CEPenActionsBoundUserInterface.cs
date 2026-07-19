using Content.Client.UserInterface.Controls;
using Content.Shared._CE.Pen;
using Content.Shared._CE.Science.Components;
using Content.Shared._CE.Science.Prototypes;
using JetBrains.Annotations;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;

namespace Content.Client._CE.Pen;

[UsedImplicitly]
public sealed partial class CEPenActionsBoundUserInterface : BoundUserInterface
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IPlayerManager _player = default!;

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
        // A lone "record knowledge" action skips its own top-level button - the achievement
        // submenu is shown directly, matching what the server does when opening the UI.
        if (actions.Count == 1 && actions[0].Kind == CEPenActionKind.RecordKnowledge)
            return BuildAchievementButtons(user);

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

                case CEPenActionKind.RecordKnowledge:
                    var nested = BuildAchievementButtons(user);
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

    private List<RadialMenuOptionBase> BuildAchievementButtons(EntityUid user)
    {
        var options = new List<RadialMenuOptionBase>();

        if (!EntMan.TryGetComponent<CEScienceResearchDataComponent>(user, out var research))
            return options;

        foreach (var achievementId in research.DiscoveredAchievements)
        {
            if (!_prototype.TryIndex(achievementId, out var achievement))
                continue;

            options.Add(new RadialMenuActionOption<ProtoId<CEScienceAchievementPrototype>>(HandleAchievementPicked, achievementId)
            {
                ToolTip = Loc.GetString(achievement.Name),
                IconSpecifier = RadialMenuIconSpecifier.With(achievement.Icon),
            });
        }

        return options;
    }

    private void HandleSimpleAction(CEPenActionKind kind)
    {
        SendMessage(new CEPenActionsMessage(kind));
    }

    private void HandleAchievementPicked(ProtoId<CEScienceAchievementPrototype> achievement)
    {
        SendMessage(new CEPenActionsMessage(CEPenActionKind.RecordKnowledge, achievement));
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            _menu?.Dispose();
            _menu = null;
        }
    }
}
