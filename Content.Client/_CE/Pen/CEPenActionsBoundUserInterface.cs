using Content.Client.UserInterface.Controls;
using Content.Shared._CE.Knowledge.Components;
using Content.Shared._CE.Knowledge.Prototypes;
using Content.Shared._CE.Pen;
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
        // A lone "record knowledge" action skips its own top-level button - the knowledge
        // submenu is shown directly, matching what the server does when opening the UI.
        if (actions.Count == 1 && actions[0].Kind == CEPenActionKind.RecordKnowledge)
            return BuildKnowledgeButtons(user);

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
                    var nested = BuildKnowledgeButtons(user);
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

    private List<RadialMenuOptionBase> BuildKnowledgeButtons(EntityUid user)
    {
        var options = new List<RadialMenuOptionBase>();

        if (!EntMan.TryGetComponent<CEKnowledgeComponent>(user, out var knowledgeComp))
            return options;

        foreach (var knowledgeId in knowledgeComp.Known)
        {
            if (!_prototype.TryIndex(knowledgeId, out var knowledge))
                continue;

            var texture = knowledge.GetIconTexture();

            options.Add(new RadialMenuActionOption<ProtoId<CEKnowledgePrototype>>(HandleKnowledgePicked, knowledgeId)
            {
                ToolTip = knowledge.GetTitle(_prototype),
                IconSpecifier = texture is not null
                    ? RadialMenuIconSpecifier.With(texture)
                    : RadialMenuIconSpecifier.With(knowledge.PreviewEntity),
            });
        }

        return options;
    }

    private void HandleSimpleAction(CEPenActionKind kind)
    {
        SendMessage(new CEPenActionsMessage(kind));
    }

    private void HandleKnowledgePicked(ProtoId<CEKnowledgePrototype> knowledge)
    {
        SendMessage(new CEPenActionsMessage(CEPenActionKind.RecordKnowledge, knowledge));
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
