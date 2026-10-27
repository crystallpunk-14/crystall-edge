using Robust.Client.UserInterface.CustomControls;

namespace Content.Client._CE.LockKey.UI;

public sealed class CELockpickWindow : DefaultWindow
{
    public readonly CELockpickControl Lock;

    public CELockpickWindow()
    {
        Resizable = false;
        Lock = new CELockpickControl();
        Contents.AddChild(Lock);
    }
}
