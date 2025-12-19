using Content.Shared._CE.BlueText;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client._CE.BlueText;

[UsedImplicitly]
public sealed class CEBlueTextBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private CEBlueTextMenu? _menu;

    private EntityUid _owner;

    public CEBlueTextBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        _owner = owner;
    }

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<CEBlueTextMenu>();
        _menu.OnSubmitBlueText += HandleSubmitBlueText;
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (_menu == null)
            return;

        if (state is not CEBlueTextBuiState bluetextState)
            return;

        _menu.Update(_owner, bluetextState);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (!disposing)
            return;

        if (_menu != null)
            _menu.OnSubmitBlueText -= HandleSubmitBlueText;

        _menu?.Dispose();
        _menu = null;
    }

    private void HandleSubmitBlueText(string text)
    {
        if (_menu == null)
            return;

        SendMessage(new CEBlueTextSubmitMessage(text));
    }
}
