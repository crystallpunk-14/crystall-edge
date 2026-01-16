using Content.Shared._CE.Workbench;
using Robust.Client.UserInterface;

namespace Content.Client._CE.Workbench;

public sealed class CEAutoCrafterBoundUserInterface : BoundUserInterface
{
    private CEAutoCrafterWindow? _window;

    public CEAutoCrafterBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<CEAutoCrafterWindow>();

        _window.OnSelectRecipe += entry => SendMessage(new CEWorkbenchUiClickRecipeMessage(entry.ProtoId));
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        switch (state)
        {
            case CEWorkbenchUiRecipesState autoCrafterState:
                _window?.UpdateState(autoCrafterState);
                break;
        }
    }
}
