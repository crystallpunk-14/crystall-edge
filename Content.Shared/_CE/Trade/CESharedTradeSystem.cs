using Content.Shared._CE.Currency;
using Content.Shared._CE.ResourceManager;
using Content.Shared._CE.Trade.Components;
using Content.Shared._CE.Trade.Prototypes;
using Content.Shared._CE.Trade.Rewards;
using Content.Shared.Examine;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Storage;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared._CE.Trade;

public abstract partial class CESharedTradeSystem : EntitySystem
{
    [Dependency] protected IPrototypeManager Proto = default!;
    [Dependency] protected CESharedCurrencySystem Currency = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private CEEconomySystem _economy = default!;
    [Dependency] private MetaDataSystem _meta = default!;
    [Dependency] private IRobustRandom _random = default!;

    [Dependency] private EntityQuery<StorageComponent> _storageQuery = default!;
    [Dependency] private EntityQuery<CETradeOfferComponent> _offerQuery = default!;

    private static readonly (EntProtoId Proto, int Value)[] Coins =
    [
        ("CECoinCopper1", 1),
        ("CECoinSilver1", 10),
        ("CECoinGold1", 100),
        ("CECoinPlatinum1", 1000),
    ];

    private readonly List<int> _freeSlots = new();
    private readonly List<EntityUid> _offers = new();

    /// <summary>
    /// Base value of one entity of the prototype.
    /// </summary>
    public virtual double EstimatePrice(EntProtoId proto)
    {
        return Proto.TryIndex(proto, out var indexed) ? _economy.GetEstimatedPrice(indexed) : 0;
    }

    public void GetFreeSlots(Entity<CETradeTableComponent> table, List<int> result)
    {
        result.Clear();
        for (var i = 0; i < table.Comp.Slots.Count; i++)
        {
            result.Add(i);
        }

        var children = Transform(table).ChildEnumerator;
        while (children.MoveNext(out var child))
        {
            if (_offerQuery.TryComp(child, out var offer))
                result.Remove(offer.Slot);
        }
    }

    public bool HasOffer(EntityUid table)
    {
        var children = Transform(table).ChildEnumerator;
        while (children.MoveNext(out var child))
        {
            if (_offerQuery.HasComp(child))
                return true;
        }

        return false;
    }

    public bool HasFreeSlot(Entity<CETradeTableComponent> table)
    {
        var occupied = 0;
        var children = Transform(table).ChildEnumerator;
        while (children.MoveNext(out var child))
        {
            if (_offerQuery.HasComp(child))
                occupied++;
        }

        return occupied < table.Comp.Slots.Count;
    }

    /// <summary>
    /// Spawns an offer into a random empty slot and lets the table's stock source fill it
    /// (see <see cref="CETradeTableFillOfferEvent"/>).
    /// </summary>
    public bool TryRestock(Entity<CETradeTableComponent> table)
    {
        GetFreeSlots(table, _freeSlots);
        if (_freeSlots.Count == 0)
            return false;

        var slot = _random.Pick(_freeSlots);
        var position = table.Comp.Slots[slot] + _random.NextVector2(table.Comp.SlotJitter);
        var uid = SpawnAttachedTo(table.Comp.OfferEntity, new EntityCoordinates(table, position));

        var comp = EnsureComp<CETradeOfferComponent>(uid);
        comp.Slot = slot;

        var ev = new CETradeTableFillOfferEvent(uid);
        RaiseLocalEvent(table, ref ev);

        if (!ev.Handled)
        {
            Del(uid);
            return false;
        }

        return true;
    }

    /// <summary>
    /// Writes the whole exchange into a freshly spawned offer.
    /// </summary>
    /// <param name="name">Offer name; defaults to the first reward's or cost's name.</param>
    public void FillOffer(EntityUid uid,
        List<CEResourceRequirement> cost,
        List<CETradeReward> reward,
        int payPrice,
        int receivePrice,
        EntProtoId? preview = null,
        string? name = null)
    {
        if (!_offerQuery.TryComp(uid, out var comp))
            return;

        comp.Cost = new List<CEResourceRequirement>(cost);
        comp.Reward = new List<CETradeReward>(reward);
        comp.PayPrice = payPrice;
        comp.ReceivePrice = receivePrice;
        comp.Preview = preview;
        Dirty(uid, comp);

        _meta.SetEntityName(uid, name ?? GetOfferName(comp.Reward, comp.Cost) ?? string.Empty);
        if (comp.Reward.Count > 0 && GetPreview(comp) is { } previewId && Proto.TryIndex(previewId, out var previewProto))
            _meta.SetEntityDescription(uid, previewProto.Description);
    }

    [SubscribeLocalEvent]
    private void OnShopTableFill(Entity<CETradeShopTableComponent> ent, ref CETradeTableFillOfferEvent args)
    {
        if (args.Handled)
            return;

        if (PickOffer(ent.Comp.Shop) is not { } offer)
            return;

        FillOffer(args.Offer,
            offer.Cost,
            offer.Reward,
            RollPrice(offer.Pay, offer, pay: true),
            RollPrice(offer.Receive, offer, pay: false),
            offer.Preview,
            GetOfferName(offer));

        args.Handled = true;
    }

    /// <summary>
    /// Removes a random offer from the table.
    /// </summary>
    public bool TryClearOne(Entity<CETradeTableComponent> table)
    {
        _offers.Clear();
        var children = Transform(table).ChildEnumerator;
        while (children.MoveNext(out var child))
        {
            if (_offerQuery.HasComp(child))
                _offers.Add(child);
        }

        if (_offers.Count == 0)
            return false;

        QueueDel(_random.Pick(_offers));
        return true;
    }

    private CETradeOfferPrototype? PickOffer(ProtoId<CETradeShopPrototype> shop)
    {
        var total = 0f;
        foreach (var offer in Proto.EnumeratePrototypes<CETradeOfferPrototype>())
        {
            if (offer.Shop == shop)
                total += offer.Weight;
        }

        if (total <= 0)
            return null;

        var roll = _random.NextFloat() * total;
        CETradeOfferPrototype? last = null;
        foreach (var offer in Proto.EnumeratePrototypes<CETradeOfferPrototype>())
        {
            if (offer.Shop != shop)
                continue;

            last = offer;
            roll -= offer.Weight;
            if (roll <= 0)
                return offer;
        }

        return last;
    }

    private int RollPrice(CETradeMoney? money, CETradeOfferPrototype offer, bool pay)
    {
        if (money is null)
            return 0;

        double price;
        if (money.Amount is { } amount)
        {
            price = amount;
        }
        else
        {
            price = 0;
            if (pay)
            {
                foreach (var reward in offer.Reward)
                {
                    price += reward.GetPrice(EntityManager);
                }
            }
            else
            {
                foreach (var cost in offer.Cost)
                {
                    price += cost.GetPrice(EntityManager, Proto);
                }
            }

            price = price * money.Markup + money.Bonus;
        }

        price *= 1 + _random.NextFloat(-money.Variation, money.Variation);
        return Math.Max(1, (int) Math.Round(price));
    }

    /// <summary>
    /// Items the user can trade away: held items, pocket items and the contents of any carried storage.
    /// Worn equipment itself is never included.
    /// </summary>
    public HashSet<EntityUid> CollectTradeableItems(EntityUid user)
    {
        var result = new HashSet<EntityUid>();

        foreach (var held in _hands.EnumerateHeld(user))
        {
            AddWithContents(held, result, true);
        }

        if (_inventory.TryGetContainerSlotEnumerator(user, out var slots))
        {
            while (slots.NextItem(out var item, out var slot))
            {
                AddWithContents(item, result, (slot.SlotFlags & SlotFlags.POCKET) != 0);
            }
        }

        return result;
    }

    private void AddWithContents(EntityUid uid, HashSet<EntityUid> result, bool includeSelf)
    {
        if (includeSelf)
            result.Add(uid);

        if (!_storageQuery.TryComp(uid, out var storage))
            return;

        foreach (var contained in storage.Container.ContainedEntities)
        {
            AddWithContents(contained, result, true);
        }
    }

    public EntProtoId? GetPreview(CETradeOfferPrototype offer, int receivePrice)
    {
        return GetPreview(offer.Preview, offer.Reward, offer.Cost, receivePrice, out _);
    }

    public EntProtoId? GetPreview(CETradeOfferComponent offer)
    {
        return GetPreview(offer, out _);
    }

    /// <param name="stackCount">Stack size to display, or null to keep the prototype's own.</param>
    public EntProtoId? GetPreview(CETradeOfferComponent offer, out int? stackCount)
    {
        return GetPreview(offer.Preview, offer.Reward, offer.Cost, offer.ReceivePrice, out stackCount);
    }

    /// <summary>
    /// Entity whose sprite represents the offer: explicit preview, first reward, coins when paying out, first cost.
    /// </summary>
    private EntProtoId? GetPreview(EntProtoId? preview,
        List<CETradeReward> rewards,
        List<CEResourceRequirement> costs,
        int receivePrice,
        out int? stackCount)
    {
        stackCount = null;

        if (preview is { } explicitPreview)
            return explicitPreview;

        foreach (var reward in rewards)
        {
            if (reward.GetPreview() is { } rewardPreview)
                return rewardPreview;
        }

        if (receivePrice > 0)
        {
            var coin = GetCoinPreview(receivePrice, out var count);
            stackCount = count;
            return coin;
        }

        foreach (var cost in costs)
        {
            foreach (var layer in cost.GetRequirementIcon(EntityManager, Proto))
            {
                if (layer.Entity is { } entity)
                    return entity;
            }
        }

        return null;
    }

    /// <summary>
    /// The biggest coin denomination not exceeding the amount, and how many of those coins fit into it.
    /// </summary>
    public static EntProtoId GetCoinPreview(int amount, out int count)
    {
        for (var i = Coins.Length - 1; i >= 0; i--)
        {
            if (Coins[i].Value > amount && i > 0)
                continue;

            count = Math.Max(1, amount / Coins[i].Value);
            return Coins[i].Proto;
        }

        count = 1;
        return Coins[0].Proto;
    }

    public string GetOfferName(CETradeOfferPrototype offer)
    {
        if (offer.Name is { } name)
            return Loc.GetString(name);

        return GetOfferName(offer.Reward, offer.Cost) ?? offer.ID;
    }

    private string? GetOfferName(List<CETradeReward> rewards, List<CEResourceRequirement> costs)
    {
        foreach (var reward in rewards)
        {
            return reward.GetName(Proto);
        }

        foreach (var cost in costs)
        {
            return cost.GetRequirementTitle(Proto);
        }

        return null;
    }

    [SubscribeLocalEvent]
    private void OnOfferExamined(Entity<CETradeOfferComponent> ent, ref ExaminedEvent args)
    {
        var offer = ent.Comp;

        using (args.PushGroup(nameof(CETradeOfferComponent)))
        {
            if (offer.Cost.Count > 0 || ent.Comp.PayPrice > 0)
            {
                args.PushMarkup(Loc.GetString("ce-trade-examine-cost"));
                foreach (var cost in offer.Cost)
                {
                    args.PushMarkup(Loc.GetString("ce-trade-examine-entry", ("name", cost.GetRequirementTitle(Proto))));
                }

                if (ent.Comp.PayPrice > 0)
                    args.PushMarkup(Loc.GetString("ce-trade-examine-entry", ("name", Currency.GetCurrencyPrettyString(ent.Comp.PayPrice))));
            }

            if (offer.Reward.Count > 0 || ent.Comp.ReceivePrice > 0)
            {
                args.PushMarkup(Loc.GetString("ce-trade-examine-reward"));
                foreach (var reward in offer.Reward)
                {
                    args.PushMarkup(Loc.GetString("ce-trade-examine-entry", ("name", reward.GetName(Proto))));
                }

                if (ent.Comp.ReceivePrice > 0)
                    args.PushMarkup(Loc.GetString("ce-trade-examine-entry", ("name", Currency.GetCurrencyPrettyString(ent.Comp.ReceivePrice))));
            }
        }
    }
}

/// <summary>
/// Broadcast after a player completes an exchange with a trade offer.
/// </summary>
[ByRefEvent]
public readonly record struct CETradeCompletedEvent(
    EntityUid Buyer,
    EntityUid Offer,
    int Paid,
    int Received);
