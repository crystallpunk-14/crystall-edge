using Robust.Client.UserInterface;

namespace Content.Client._CE.Map;

public sealed class CEMapBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private CEMapWindow? _window;

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<CEMapWindow>();
        _window.Title = EntMan.GetComponent<MetaDataComponent>(Owner).EntityName;

        var mapUid = EntMan.TryGetComponent<TransformComponent>(Owner, out var xform) ? xform.MapUid : null;
        _window.SetMap(Owner, mapUid);
    }
}
