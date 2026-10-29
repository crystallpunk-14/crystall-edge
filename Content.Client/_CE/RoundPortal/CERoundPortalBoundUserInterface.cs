using Content.Shared._CE.RoundPortal;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client._CE.RoundPortal;

[UsedImplicitly]
public sealed class CERoundPortalBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private CERoundPortalWindow? _window;

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<CERoundPortalWindow>();
        _window.Title = EntMan.GetComponent<MetaDataComponent>(Owner).EntityName;
        _window.LeaveButton.OnPressed += _ => SendMessage(new CERoundPortalLeaveMessage());
        _window.CancelButton.OnPressed += _ => Close();
    }
}
