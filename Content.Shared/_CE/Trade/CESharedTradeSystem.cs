using Content.Shared._CE.Currency;
using Content.Shared._CE.Trade.Components;
using Content.Shared._CE.Trade.Prototypes;
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
    /// Places a weighted random offer of the table's shop into a random empty slot.
    /// </summary>
    public bool TryRestock(Entity<CETradeTableComponent> table)
    {
        GetFreeSlots(table, _freeSlots);
        if (_freeSlots.Count == 0)
            return false;

        if (PickOffer(table.Comp.Shop) is not { } offer)
            return false;

        var slot = _random.Pick(_freeSlots);
        var position = table.Comp.Slots[slot] + _random.NextVector2(table.Comp.SlotJitter);
        var uid = SpawnAttachedTo(table.Comp.OfferEntity, new EntityCoordinates(table, position));

        var comp = EnsureComp<CETradeOfferComponent>(uid);
        comp.Offer = offer.ID;
        comp.Slot = slot;
        comp.PayPrice = RollPrice(offer.Pay, offer, pay: true);
        comp.ReceivePrice = RollPrice(offer.Receive, offer, pay: false);
        Dirty(uid, comp);

        _meta.SetEntityName(uid, GetOfferName(offer));
        if (offer.Reward.Count > 0 && GetPreview(offer, 0) is { } preview && Proto.TryIndex(preview, out var previewProto))
            _meta.SetEntityDescription(uid, previewProto.Description);

        return true;
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
        return GetPreview(offer, receivePrice, out _);
    }

    /// <summary>
    /// Entity whose sprite represents the offer: explicit preview, first reward, coins when paying out, first cost.
    /// </summary>
    /// <param name="stackCount">Stack size to display, or null to keep the prototype's own.</param>
    public EntProtoId? GetPreview(CETradeOfferPrototype offer, int receivePrice, out int? stackCount)
    {
        stackCount = null;

        if (offer.Preview is { } preview)
            return preview;

        foreach (var reward in offer.Reward)
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

        foreach (var cost in offer.Cost)
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

        foreach (var reward in offer.Reward)
        {
            return reward.GetName(Proto);
        }

        foreach (var cost in offer.Cost)
        {
            return cost.GetRequirementTitle(Proto);
        }

        return offer.ID;
    }

    [SubscribeLocalEvent]
    private void OnOfferExamined(Entity<CETradeOfferComponent> ent, ref ExaminedEvent args)
    {
        if (!Proto.Resolve(ent.Comp.Offer, out var offer))
            return;

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
    ProtoId<CETradeShopPrototype> Shop,
    ProtoId<CETradeOfferPrototype> Offer,
    int Paid,
    int Received);
