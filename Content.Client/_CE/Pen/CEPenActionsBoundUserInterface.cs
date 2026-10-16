using Content.Client.UserInterface.Controls;
using Content.Shared._CE.Pen;
using JetBrains.Annotations;
using Robust.Client.Player;
using Robust.Client.UserInterface;

namespace Content.Client._CE.Pen;

[UsedImplicitly]
public sealed partial class CEPenActionsBoundUserInterface : BoundUserInterface
{
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

        var models = BuildButtons(actions);
        if (models.Count == 0)
            return;

        _menu = this.CreateWindow<SimpleRadialMenu>();
        _menu.Track(Owner);
        _menu.SetButtons(models);
        _menu.OpenOverMouseScreenPosition();
    }

    private List<RadialMenuOptionBase> BuildButtons(List<CEPenAction> actions)
    {
        var options = new List<RadialMenuOptionBase>();

        foreach (var action in actions)
        {
            options.Add(new RadialMenuActionOption<CEPenActionKind>(HandleSimpleAction, action.Kind)
            {
                ToolTip = Loc.GetString(action.Name),
                IconSpecifier = RadialMenuIconSpecifier.With(action.Icon),
            });
        }

        return options;
    }

    private void HandleSimpleAction(CEPenActionKind kind)
    {
        SendMessage(new CEPenActionsMessage(kind));
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
