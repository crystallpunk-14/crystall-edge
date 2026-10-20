using System.Linq;
using Content.Server.Cargo.Systems;
using Content.Server.Popups;
using Content.Server.Stack;
using Content.Shared._CE.Currency;
using Content.Shared.Examine;
using Content.Shared.Whitelist;
using Robust.Server.Audio;
using Robust.Shared.Analyzers;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Server._CE.Currency;

public sealed partial class CECurrencySystem : CESharedCurrencySystem
{
    [Dependency] private PopupSystem _popup = default!;
    [Dependency] private EntityWhitelistSystem _whitelist = default!;
    [Dependency] private StackSystem _stack = default!;
    [Dependency] private AudioSystem _audio = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private PricingSystem _price = default!;

    [SubscribeLocalEvent]
    private void OnExamine(Entity<CECurrencyExaminableComponent> currency, ref ExaminedEvent args)
    {
        var price = _price.GetPrice(currency);
        var push = Loc.GetString("ce-currency-examine-title");
        push += GetCurrencyPrettyString((int)price);
        args.PushMarkup(push);
    }

    /// <summary>
    /// Takes <paramref name="amount"/> worth of coins from anywhere in the player's containers,
    /// greedily spending the highest denominations first.
    /// Returns false (and takes nothing) if the player doesn't have enough.
    /// </summary>
    /// <param name="change">Overpaid remainder the caller owes the player back.</param>
    public bool TryTakeCurrency(EntityUid player, int amount, out int change)
    {
        change = 0;
        if (amount <= 0)
            return true;

        if (!ContainerQuery.TryGetComponent(player, out var initial))
            return false;

        var coins = new List<(EntityUid Uid, int UnitPrice, int Count)>();
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
                    coins.Add((contained, unitPrice, count));
                }

                if (ContainerQuery.TryGetComponent(contained, out var nested))
                    containerStack.Push(nested);
            }
        } while (containerStack.Count > 0);

        if (coins.Sum(c => c.UnitPrice * c.Count) < amount)
            return false;

        // Greedy: highest denomination first
        coins.Sort((a, b) => b.UnitPrice.CompareTo(a.UnitPrice));

        var remaining = amount;
        var overpaid = 0;

        foreach (var (uid, unitPrice, count) in coins)
        {
            if (remaining <= 0)
                break;

            var totalValue = unitPrice * count;
            if (totalValue <= remaining)
            {
                remaining -= totalValue;
                QueueDel(uid);
            }
            else
            {
                var coinsNeeded = (remaining + unitPrice - 1) / unitPrice; // ceiling division
                overpaid = coinsNeeded * unitPrice - remaining;
                remaining = 0;

                if (coinsNeeded >= count)
                    QueueDel(uid);
                else
                    _stack.SetCount(uid, count - coinsNeeded);
            }
        }

        change = overpaid;
        return true;
    }
}
