using Content.Shared._CE.Trading;
using Content.Shared._CE.Trading.Systems;
using Robust.Client.UserInterface;

namespace Content.Client._CE.Trading;

public sealed class CETradingPlatformBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private CETradingPlatformWindow? _window;

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<CETradingPlatformWindow>();

        _window.OnBuy += pos => SendMessage(new CETradingPositionBuyAttempt(pos));
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        switch (state)
        {
            case CETradingPlatformUiState storeState:
                _window?.UpdateState(storeState);
                break;
        }
    }
}
