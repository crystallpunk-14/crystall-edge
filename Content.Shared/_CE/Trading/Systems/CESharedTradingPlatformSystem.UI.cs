using Content.Shared._CE.Trading.Prototypes;

namespace Content.Shared._CE.Trading.Systems;

public abstract partial class CESharedTradingPlatformSystem
{
    public string GetTradeDescription(CETradingPositionPrototype position)
    {
        if (position.Desc != null)
            return Loc.GetString(position.Desc);

        return position.Service.GetDesc(Proto);
    }

    public string GetTradeName(CETradingPositionPrototype position)
    {
        if (position.Name != null)
            return Loc.GetString(position.Name);

        return position.Service.GetName(Proto);
    }
}
