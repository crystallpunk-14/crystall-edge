using Content.Shared._CE.Bluetext;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Shared.ViewVariables;

namespace Content.Client._CE.Bluetext;

[UsedImplicitly]
public sealed class CEBluetextBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private CEBluetextMenu? _menu;

    private EntityUid _owner;

    public CEBluetextBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        _owner = owner;
    }

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<CEBluetextMenu>();
        _menu.OnSubmitBluetext += HandleSubmitBluetext;
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (_menu == null)
            return;

        if (state is not CEBluetextBuiState bluetextState)
            return;

        _menu.Update(_owner, bluetextState);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (!disposing)
            return;

        if (_menu != null)
        {
            _menu.OnSubmitBluetext -= HandleSubmitBluetext;
        }

        _menu?.Dispose();
        _menu = null;
    }

    private void HandleSubmitBluetext(string text)
    {
        if (_menu == null)
            return;

        SendMessage(new CEBluetextSubmitMessage(text));
    }
}
