using System.Linq;
using System.Numerics;
using Content.Server._CE.Currency;
using Content.Server.Cargo.Systems;
using Content.Server.Stack;
using Content.Shared._CE.Trade;
using Content.Shared._CE.Trade.Components;
using Content.Shared.Hands;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Popups;
using Content.Shared.Stacks;
using Content.Shared.Storage;
using Content.Shared.Storage.EntitySystems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server._CE.Trade;

public sealed partial class CETradeSystem : CESharedTradeSystem
{
    [Dependency] private PricingSystem _pricing = default!;
    [Dependency] private CECurrencySystem _currency = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private SharedStorageSystem _storage = default!;
    [Dependency] private StackSystem _stack = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SharedPhysicsSystem _physics = default!;
    [Dependency] private IRobustRandom _random = default!;

    [Dependency] private EntityQuery<StackComponent> _stackQuery = default!;
    [Dependency] private EntityQuery<StorageComponent> _storageQuery = default!;
    [Dependency] private EntityQuery<PhysicsComponent> _physicsQuery = default!;

    // Scatter of items that don't fit into the buyer's inventory, same as pot loot.
    private const float ScatterOffset = 0.3f;
    private const float ScatterMinSpeed = 0.5f;
    private const float ScatterMaxSpeed = 2f;

    public override double EstimatePrice(EntProtoId proto)
    {
        var temp = Spawn(proto, MapCoordinates.Nullspace);
        var price = _pricing.GetPrice(temp);
        Del(temp);
        return price;
    }

    [SubscribeLocalEvent]
    private void OnRestockEvent(Entity<CETradeTableComponent> ent, ref CERestockTradeTableEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = TryRestock(ent);
    }

    [SubscribeLocalEvent]
    private void OnClearEvent(Entity<CETradeTableComponent> ent, ref CEClearTradeTableEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = TryClearOne(ent);
    }

    [SubscribeLocalEvent]
    private void OnInteractHand(Entity<CETradeOfferComponent> ent, ref InteractHandEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        TryTrade(ent, args.User);
    }

    /// <summary>
    /// Takes the offer's cost from the user and gives them its reward. Fails without taking anything
    /// if the user can't pay in full.
    /// </summary>
    public bool TryTrade(Entity<CETradeOfferComponent> ent, EntityUid user)
    {
        var offer = ent.Comp;
        var items = CollectTradeableItems(user);

        var missing = new List<string>();
        foreach (var cost in offer.Cost)
        {
            if (!cost.CheckRequirement(EntityManager, Proto, items))
                missing.Add(cost.GetRequirementTitle(Proto));
        }

        if (ent.Comp.PayPrice > 0 && Currency.GetPriceTotal(user) < ent.Comp.PayPrice)
            missing.Add(FormattedMessage.RemoveMarkupPermissive(Currency.GetCurrencyPrettyString(ent.Comp.PayPrice)).Trim());

        if (missing.Count > 0)
        {
            _popup.PopupEntity(Loc.GetString("ce-trade-missing", ("items", string.Join(", ", missing))), ent, user, PopupType.SmallCaution);
            return false;
        }

        if (!_currency.TryTakeCurrency(user, ent.Comp.PayPrice, out var change))
            return false;

        foreach (var cost in offer.Cost)
        {
            cost.PostCraft(EntityManager, Proto, items);
        }

        var mapCoords = _transform.GetMapCoordinates(ent);
        var given = new List<EntityUid>();
        foreach (var reward in offer.Reward)
        {
            reward.Give(EntityManager, mapCoords, given);
        }

        var money = ent.Comp.ReceivePrice + change;
        if (money > 0)
            given.AddRange(_currency.GenerateMoney(money, _transform.ToCoordinates(mapCoords)));

        var coords = Transform(ent).Coordinates;
        _audio.PlayPvs(ent.Comp.TradeSound, coords);
        Deliver(given, user, mapCoords, coords);

        var ev = new CETradeCompletedEvent(user, ent, ent.Comp.PayPrice, ent.Comp.ReceivePrice);
        RaiseLocalEvent(ref ev);

        QueueDel(ent);
        return true;
    }

    /// <summary>
    /// Puts the given entities into the buyer's inventory: existing stacks first, then storages, then free hands.
    /// Whatever doesn't fit scatters around the offer.
    /// </summary>
    private void Deliver(List<EntityUid> items, EntityUid user, MapCoordinates offerPos, EntityCoordinates offerCoords)
    {
        var stacks = new List<Entity<StackComponent>>();
        foreach (var owned in CollectTradeableItems(user))
        {
            if (_stackQuery.TryComp(owned, out var stack))
                stacks.Add((owned, stack));
        }

        var storages = CollectStorages(user);
        var delivered = new HashSet<EntityUid>();

        foreach (var item in items)
        {
            if (!TryDeliver(item, user, stacks, storages, delivered))
                Scatter(item, offerPos);
        }

        AnimateGiven(delivered, offerCoords, user);
    }

    private bool TryDeliver(
        EntityUid item,
        EntityUid user,
        List<Entity<StackComponent>> stacks,
        List<Entity<StorageComponent>> storages,
        HashSet<EntityUid> delivered)
    {
        _stackQuery.TryComp(item, out var itemStack);
        if (itemStack != null)
        {
            foreach (var stack in stacks)
            {
                if (!_stack.TryMergeStacks((item, itemStack), stack.AsNullable(), out _))
                    continue;

                delivered.Add(stack);
                if (itemStack.Count == 0)
                    return true;
            }
        }

        var placed = false;
        foreach (var storage in storages)
        {
            // Insert itself skips whitelist and grid checks.
            if (!_storage.CanInsert(storage, item, out _, storage.Comp, ignoreStacks: true)
                || !_storage.Insert(storage, item, out _, storageComp: storage.Comp, playSound: false, stackAutomatically: false))
                continue;

            placed = true;
            break;
        }

        if (!placed && !_hands.TryPickupAnyHand(user, item, checkActionBlocker: false))
            return false;

        delivered.Add(item);
        if (itemStack != null)
            stacks.Add((item, itemStack));

        return true;
    }

    /// <summary>
    /// Storages the buyer carries, with specialized ones (wallets, quivers) ahead of generic bags.
    /// </summary>
    private List<Entity<StorageComponent>> CollectStorages(EntityUid user)
    {
        var result = new List<Entity<StorageComponent>>();

        foreach (var held in _hands.EnumerateHeld(user))
        {
            AddStorages(held, result);
        }

        if (_inventory.TryGetContainerSlotEnumerator(user, out var slots))
        {
            while (slots.NextItem(out var item))
            {
                AddStorages(item, result);
            }
        }

        return result.OrderByDescending(s => s.Comp.Whitelist != null).ToList();
    }

    private void AddStorages(EntityUid uid, List<Entity<StorageComponent>> result)
    {
        if (!_storageQuery.TryComp(uid, out var storage))
            return;

        result.Add((uid, storage));
        foreach (var contained in storage.Container.ContainedEntities)
        {
            AddStorages(contained, result);
        }
    }

    private void Scatter(EntityUid item, MapCoordinates from)
    {
        var offset = new Vector2(_random.NextFloat(-ScatterOffset, ScatterOffset), _random.NextFloat(-ScatterOffset, ScatterOffset));
        _transform.SetMapCoordinates(item, from.Offset(offset));
        _transform.SetLocalRotation(item, _random.NextAngle());

        if (_physicsQuery.TryComp(item, out var body))
            _physics.SetLinearVelocity(item, _random.NextAngle().ToVec() * _random.NextFloat(ScatterMinSpeed, ScatterMaxSpeed), body: body);
    }

    /// <summary>
    /// Shows the given entities flying from the offer into the buyer's inventory.
    /// </summary>
    private void AnimateGiven(HashSet<EntityUid> given, EntityCoordinates from, EntityUid user)
    {
        var to = GetNetCoordinates(Transform(user).Coordinates);
        var netFrom = GetNetCoordinates(from);
        var filter = Filter.Pvs(user, entityManager: EntityManager);

        foreach (var item in given)
        {
            if (Deleted(item))
                continue;

            RaiseNetworkEvent(new PickupAnimationEvent(GetNetEntity(item), netFrom, to, Angle.Zero), filter);
        }
    }
}
