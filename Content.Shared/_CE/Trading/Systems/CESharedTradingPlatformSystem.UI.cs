using Content.Shared._CE.Trading.Components;
using Content.Shared._CE.Trading.Prototypes;
using Content.Shared.UserInterface;

namespace Content.Shared._CE.Trading.Systems;

public abstract partial class CESharedTradingPlatformSystem
{
    private void InitializeUI()
    {
        SubscribeLocalEvent<CETradingPlatformComponent, BeforeActivatableUIOpenEvent>(OnBeforeTradingUIOpen);
    }

    private void OnBeforeTradingUIOpen(Entity<CETradingPlatformComponent> ent, ref BeforeActivatableUIOpenEvent args)
    {
        UpdateTradingUIState(ent, args.User);
    }

    protected void UpdateTradingUIState(Entity<CETradingPlatformComponent> ent, EntityUid user)
    {
        _userInterface.SetUiState(ent.Owner, CETradingUiKey.Buy, new CETradingPlatformUiState(GetNetEntity(ent)));
    }

    public string GetTradeDescription(CETradingPositionPrototype position)
    {
        if (position.Desc != null)
            return Loc.GetString(position.Desc);

        if (position.Service is null)
            return string.Empty;

        return position.Service.GetDesc(Proto);
    }

    public string GetTradeName(CETradingPositionPrototype position)
    {
        if (position.Name != null)
            return Loc.GetString(position.Name);

        if (position.Service is null)
            return string.Empty;

        return position.Service.GetName(Proto);
    }
}
