using Content.Shared._CE.Trade;
using Robust.Client.UserInterface;

namespace Content.Client._CE.Trade;

public sealed class CETradeShopBoundUserInterface : BoundUserInterface
{
    private CETradeShopWindow? _window;

    public CETradeShopBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<CETradeShopWindow>();
        _window.Title = EntMan.GetComponent<MetaDataComponent>(Owner).EntityName;
        _window.OnTrade += offer => SendMessage(new CETradeShopTradeMessage(offer));
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is CETradeShopUiState shopState)
            _window?.UpdateState(shopState);
    }
}
