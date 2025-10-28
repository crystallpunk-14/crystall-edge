/*
 * This file is sublicensed under MIT License
 * https://github.com/space-wizards/space-station-14/blob/master/LICENSE.TXT
 */

using Content.Shared._CE.Workbench;
using Robust.Client.UserInterface;

namespace Content.Client._CE.Workbench;

public sealed class CEWorkbenchBoundUserInterface : BoundUserInterface
{
    private CEWorkbenchWindow? _window;

    public CEWorkbenchBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<CEWorkbenchWindow>();

        _window.OnCraft += entry => SendMessage(new CEWorkbenchUiCraftMessage(entry.ProtoId));
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        switch (state)
        {
            case CEWorkbenchUiRecipesState recipesState:
                _window?.UpdateState(recipesState);
                break;
        }
    }
}
