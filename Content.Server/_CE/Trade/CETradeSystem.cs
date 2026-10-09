using Content.Server._CE.Currency;
using Content.Server.Cargo.Systems;
using Content.Shared._CE.Trade;
using Content.Shared._CE.Trade.Components;
using Content.Shared.Hands;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server._CE.Trade;

public sealed partial class CETradeSystem : CESharedTradeSystem
{
    [Dependency] private PricingSystem _pricing = default!;
    [Dependency] private CECurrencySystem _currency = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedHandsSystem _hands = default!;

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

        if (!_currency.TryTakeCurrency(user, ent.Comp.PayPrice))
            return false;

        foreach (var cost in offer.Cost)
        {
            cost.PostCraft(EntityManager, Proto, items);
        }

        var given = new List<EntityUid>();
        foreach (var reward in offer.Reward)
        {
            reward.Give(EntityManager, user, given);
        }

        if (ent.Comp.ReceivePrice > 0)
        {
            foreach (var coin in _currency.GenerateMoney(ent.Comp.ReceivePrice, Transform(user).Coordinates))
            {
                _hands.TryPickupAnyHand(user, coin, checkActionBlocker: false);
                given.Add(coin);
            }
        }

        var coords = Transform(ent).Coordinates;
        _audio.PlayPvs(ent.Comp.TradeSound, coords);
        AnimateGiven(given, coords, user);

        var ev = new CETradeCompletedEvent(user, ent, ent.Comp.PayPrice, ent.Comp.ReceivePrice);
        RaiseLocalEvent(ref ev);

        QueueDel(ent);
        return true;
    }

    /// <summary>
    /// Shows the given entities flying from the offer into the buyer's hands.
    /// </summary>
    private void AnimateGiven(List<EntityUid> given, EntityCoordinates from, EntityUid user)
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
