using System.Text;
using Content.Shared.Cargo.Components;
using Content.Shared.Stacks;
using Content.Shared.Tag;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Currency;

public abstract partial class CESharedCurrencySystem : EntitySystem
{
    public static readonly KeyValuePair<EntProtoId, int> CP = new("CECoinCopper1", 1);
    public static readonly KeyValuePair<EntProtoId, int> SP = new("CECoinSilver1", 10);
    public static readonly KeyValuePair<EntProtoId, int> GP = new("CECoinGold1", 100);
    public static readonly KeyValuePair<EntProtoId, int> PP = new("CECoinPlatinum1", 1000);

    public static readonly ProtoId<TagPrototype> CoinTag = "CECoin";

    [Dependency] protected TagSystem TagSys = default!;
    [Dependency] protected EntityQuery<ContainerManagerComponent> ContainerQuery = default!;
    [Dependency] protected EntityQuery<StackComponent> StackQuery = default!;
    [Dependency] private EntityQuery<StackPriceComponent> _stackPriceQuery = default!;
    [Dependency] private EntityQuery<StaticPriceComponent> _staticPriceQuery = default!;

    /// <summary>
    /// Sums the value of every coin held anywhere in the player's containers (hands, pockets,
    /// backpack, wallet, nested storage, etc), without taking anything. Runs identically on
    /// client and server, so the client can predict its own balance for UI display.
    /// </summary>
    public int GetPriceTotal(EntityUid player)
    {
        if (!ContainerQuery.TryGetComponent(player, out var initial))
            return 0;

        var total = 0;
        var containerStack = new Stack<ContainerManagerComponent>();
        containerStack.Push(initial);

        do
        {
            var current = containerStack.Pop();
            foreach (var container in current.Containers.Values)
            foreach (var contained in container.ContainedEntities)
            {
                var unitPrice = GetUnitPrice(contained);
                if (unitPrice > 0)
                {
                    var count = StackQuery.TryGetComponent(contained, out var stack) ? stack.Count : 1;
                    total += unitPrice * count;
                }

                if (ContainerQuery.TryGetComponent(contained, out var nested))
                    containerStack.Push(nested);
            }
        } while (containerStack.Count > 0);

        return total;
    }

    /// <summary>
    /// Only entities tagged as actual coins count as currency - most equipment also carries a
    /// StackPrice/StaticPrice (for appraisal/cargo sale) but isn't spendable money.
    /// </summary>
    protected int GetUnitPrice(EntityUid uid)
    {
        if (!TagSys.HasTag(uid, CoinTag))
            return 0;

        if (_stackPriceQuery.TryGetComponent(uid, out var sp))
            return (int) sp.Price;
        if (_staticPriceQuery.TryGetComponent(uid, out var fp))
            return (int) fp.Price;
        return 0;
    }

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
