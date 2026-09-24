using Content.Shared._CE.MurkSphere;
using Robust.Client.UserInterface;

namespace Content.Client._CE.MurkSphere;

public sealed class CEMurkSphereFixerMonitorBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private CEMurkSphereFixerMonitorWindow? _window;

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<CEMurkSphereFixerMonitorWindow>();
        _window.Title = EntMan.GetComponent<MetaDataComponent>(Owner).EntityName;

        var mapUid = EntMan.TryGetComponent<TransformComponent>(Owner, out var xform) ? xform.MapUid : null;
        _window.SetMap(mapUid);
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is CEMurkSphereFixerMonitorBoundUserInterfaceState fixerState)
            _window?.UpdateState(fixerState);
    }
}
