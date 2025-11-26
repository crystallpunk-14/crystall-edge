using Content.Shared._CE.Ambitions;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client._CE.Ambitions;

[UsedImplicitly]
public sealed class CEAmbitionsBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private CEAmbitionsMenu? _menu;

    private EntityUid _owner;

    public CEAmbitionsBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        _owner = owner;
    }

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<CEAmbitionsMenu>();

        _menu.OnNewAmbitionRequest += () => SendMessage(new CEAmbitionCreateMessage());
        _menu.OnLockAmbitionRequest += () => SendMessage(new CEAmbitionLockMessage());
        _menu.OnDeleteAmbitionRequest += (index) => SendMessage(new CEAmbitionDeleteMessage(index));
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (_menu == null)
            return;

        if (state is not CEAmbitionsBuiState msg)
            return;

        _menu.Update(_owner, msg);
    }
}
