using Content.Shared._CE.Trading;
using Content.Shared._CE.Trading.Systems;
using Robust.Client.UserInterface;

namespace Content.Client._CE.Trading.Selling;

public sealed class CESellingPlatformBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private CESellingPlatformWindow? _window;

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<CESellingPlatformWindow>();

        _window.OnSell += () => SendMessage(new CETradingSellAttempt());
        _window.OnRequestSell += pair => SendMessage(new CETradingRequestSellAttempt(pair.Item1, pair.Item2));
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        switch (state)
        {
            case CESellingPlatformUiState storeState:
                _window?.UpdateState(storeState);
                break;
        }
    }
}
