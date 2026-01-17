using Content.Shared._CE.Trading;
using Content.Shared._CE.Trading.Systems;
using Robust.Client.UserInterface;

namespace Content.Client._CE.Trading;

public sealed class CETradingPlatformBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private CETradingPlatformWindow? _window;
    private CETradingPlatformUiState? _cachedState;

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<CETradingPlatformWindow>();

        _window.OnBuy += pos => SendMessage(new CETradingBuyAttempt(pos));
        _window.OnSell += () => SendMessage(new CETradingSellAttempt());
        _window.OnRequestSell += req =>
        {
            if (_cachedState == null)
                return;

            SendMessage(new CETradingRequestSellAttempt(req, _cachedState.Faction));
        };
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        switch (state)
        {
            case CETradingPlatformUiState storeState:
                _cachedState = storeState;
                _window?.UpdateState(storeState);
                break;
        }
    }
}
