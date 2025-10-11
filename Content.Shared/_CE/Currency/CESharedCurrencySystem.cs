using System.Text;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Currency;

public partial class CESharedCurrencySystem : EntitySystem
{
    public static readonly KeyValuePair<EntProtoId, int> CP = new("CECoinCopper1", 1);
    public static readonly KeyValuePair<EntProtoId, int> SP = new("CECoinSilver1", 10);
    public static readonly KeyValuePair<EntProtoId, int> GP = new("CECoinGold1", 100);
    public static readonly KeyValuePair<EntProtoId, int> PP = new("CECoinPlatinum1", 1000);

    public string GetCurrencyPrettyString(int currency)
    {
        var total = currency;

        var sb = new StringBuilder();

        var gp = total / 100;
        total %= 100;

        var sp = total / 10;
        total %= 10;

        var cp = total;

        if (gp > 0)
            sb.Append(" " + Loc.GetString("ce-currency-examine-gp", ("coin", gp)));
        if (sp > 0)
            sb.Append(" " + Loc.GetString("ce-currency-examine-sp", ("coin", sp)));
        if (cp > 0)
            sb.Append(" " + Loc.GetString("ce-currency-examine-cp", ("coin", cp)));
        if (gp <= 0 && sp <= 0 && cp <= 0)
            sb.Append(" " + Loc.GetString("ce-trading-empty-price"));

        return sb.ToString();
    }
}
