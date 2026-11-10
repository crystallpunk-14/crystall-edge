using Content.Shared._CE.PulseAmulet;
using Robust.Client.UserInterface;

namespace Content.Client._CE.PulseAmulet;

public sealed class CEPulseProjectorBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private CEPulseProjectorWindow? _window;

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<CEPulseProjectorWindow>();
        _window.Title = EntMan.GetComponent<MetaDataComponent>(Owner).EntityName;

        var mapUid = EntMan.TryGetComponent<TransformComponent>(Owner, out var xform) ? xform.MapUid : null;
        _window.SetMap(mapUid);
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is CEPulseProjectorBoundUserInterfaceState pulseState)
            _window?.UpdateState(pulseState);
    }
}
