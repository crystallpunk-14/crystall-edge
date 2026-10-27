using Content.Shared._CE.LockKey;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client._CE.LockKey.UI;

[UsedImplicitly]
public sealed class CELockpickBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private CELockpickWindow? _window;

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<CELockpickWindow>();
        _window.Title = EntMan.GetComponent<MetaDataComponent>(Owner).EntityName;
        _window.Lock.SetLock(Owner);
        _window.Lock.OnPowerPressed += power => SendPredictedMessage(new CELockpickPressMessage(power));
    }
}
