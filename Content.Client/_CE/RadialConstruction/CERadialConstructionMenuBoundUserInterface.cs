using Content.Client.UserInterface.Controls;
using Content.Shared._CE.RadialConstruction;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;

namespace Content.Client._CE.RadialConstruction;

[UsedImplicitly]
public sealed class CERadialConstructionMenuBoundUserInterface : BoundUserInterface
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    private SimpleRadialMenu? _menu;

    public CERadialConstructionMenuBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        IoCManager.InjectDependencies(this);
    }

    protected override void Open()
    {
        base.Open();

        if (!EntMan.TryGetComponent<CERadialConstructionComponent>(Owner, out var component))
            return;

        _menu = this.CreateWindow<SimpleRadialMenu>();
        _menu.Track(Owner);
        var models = ConvertToButtons(component.AvailablePrototypes);
        _menu.SetButtons(models);

        _menu.OpenOverMouseScreenPosition();
    }

    private IEnumerable<RadialMenuOptionBase> ConvertToButtons(List<EntProtoId> prototypes)
    {
        var options = new List<RadialMenuOptionBase>();

        foreach (var protoId in prototypes)
        {
            if (!_prototypeManager.TryIndex(protoId, out var entProto))
                continue;

            var icon = RadialMenuIconSpecifier.With(protoId);

            var actionOption = new RadialMenuActionOption<EntProtoId>(HandleMenuOptionClick, protoId)
            {
                IconSpecifier = icon,
                ToolTip = entProto.Name
            };
            options.Add(actionOption);
        }

        return options;
    }

    private void HandleMenuOptionClick(EntProtoId protoId)
    {
        SendMessage(new CERadialConstructionMessage(protoId));
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
