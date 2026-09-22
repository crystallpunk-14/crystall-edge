using Content.Server._CE.Currency;
using Content.Server.Cargo.Systems;
using Content.Server.Power.EntitySystems;
using Content.Shared._CE.Trading;
using Content.Shared._CE.Trading.Components;
using Content.Shared._CE.Trading.Prototypes;
using Content.Shared._CE.Trading.Systems;
using Content.Shared.Mobs.Components;
using Content.Shared.Placeable;
using Content.Shared.Popups;
using Content.Shared.Power.Components;
using Content.Shared.Storage;
using Content.Shared.Storage.Components;
using Content.Shared.Tag;
using Content.Shared.UserInterface;
using Robust.Server.GameObjects;
using Robust.Shared.Analyzers;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;

namespace Content.Server._CE.Trading;

public sealed partial class CETradingPlatformSystem : CESharedTradingPlatformSystem
{
    [Dependency] private TagSystem _tag = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private PricingSystem _price = default!;
    [Dependency] private CECurrencySystem _currency = default!;
    [Dependency] private CEEconomySystem _economy = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private UserInterfaceSystem _userInterface = default!;

    public static readonly ProtoId<TagPrototype> CoinTag = "CECoin";

    [SubscribeLocalEvent]
    private void OnItemPlaced(Entity<CETradingPlatformComponent> ent, ref ItemPlacedEvent args)
    {
        UpdatePlatformUIState(ent);
    }

    [SubscribeLocalEvent]
    private void OnItemRemoved(Entity<CETradingPlatformComponent> ent, ref ItemRemovedEvent args)
    {
        UpdatePlatformUIState(ent);
    }

    [SubscribeLocalEvent]
    private void OnBeforeActivatableUIOpen(Entity<CETradingPlatformComponent> ent, ref BeforeActivatableUIOpenEvent args)
    {
        UpdatePlatformUIState(ent);
    }

    private void UpdatePlatformUIState(Entity<CETradingPlatformComponent> ent)
    {
        // Calculate sell balance
        double sellBalance = 0;
        if (TryComp<ItemPlacerComponent>(ent, out var itemPlacer))
        {
            foreach (var placed in itemPlacer.PlacedEntities)
            {
                if (!CanSell(placed))
                    continue;

                sellBalance += _price.GetPrice(placed);
            }
        }

        var faction = ent.Comp.Faction;
        _userInterface.SetUiState(ent.Owner, CETradingUiKey.Buy, new CETradingPlatformUiState(GetNetEntity(ent), (int)sellBalance, faction));
    }

    public bool CanSell(EntityUid uid)
    {
        if (TerminatingOrDeleted(uid))
            return false;
        if (_tag.HasTag(uid, CoinTag))
            return false;
        if (HasComp<MobStateComponent>(uid))
            return false;
        if (HasComp<EntityStorageComponent>(uid))
            return false;
        if (HasComp<StorageComponent>(uid))
            return false;

        var proto = MetaData(uid).EntityPrototype;
        if (proto != null && !proto.ID.StartsWith("CE")) //Shitfix, we dont wanna sell anything vanilla (like mob organs)
            return false;

        return true;
    }

    [SubscribeLocalEvent]
    private void OnBuyAttempt(Entity<CETradingPlatformComponent> ent, ref CETradingBuyAttempt args)
    {
        if (Timing.CurTime < ent.Comp.NextBuyTime)
            return;

        if (!Proto.TryIndex(args.Position, out var indexedPosition))
            return;

        // Ensure the platform is for the same faction as the position being bought
        if (ent.Comp.Faction != indexedPosition.Faction)
            return;

        // The buyer pays out of their own wallet/inventory, not anything placed on the platform -
        // this is what lets several players shop the same platform at once.
        var price = GetPrice(args.Position) ?? 10000;
        if (!_currency.TryTakeCurrency(args.Actor, price))
            return;

        ent.Comp.NextBuyTime = Timing.CurTime + TimeSpan.FromSeconds(1f);
        Dirty(ent);

        indexedPosition.Service.Buy(EntityManager, Proto, ent);

        _audio.PlayPvs(ent.Comp.BuySound, Transform(ent).Coordinates);
        SpawnAtPosition(ent.Comp.BuyVisual, Transform(ent).Coordinates);

        UpdatePlatformUIState(ent);
    }

    [SubscribeLocalEvent]
    private void OnSellAttempt(Entity<CETradingPlatformComponent> ent, ref CETradingSellAttempt args)
    {
        if (!TryComp<ItemPlacerComponent>(ent, out var itemPlacer))
            return;

        double balance = 0;
        foreach (var placed in itemPlacer.PlacedEntities)
        {
            if (!CanSell(placed))
                continue;

            var price = _price.GetPrice(placed);

            if (price <= 0)
                continue;

            balance += _price.GetPrice(placed);
            QueueDel(placed);
        }

        if (balance <= 0)
            return;

        _audio.PlayPvs(ent.Comp.SellSound, Transform(ent).Coordinates);
        _currency.GenerateMoney(balance, Transform(ent).Coordinates);
        SpawnAtPosition(ent.Comp.SellVisual, Transform(ent).Coordinates);

        UpdatePlatformUIState(ent);
    }

    [SubscribeLocalEvent]
    private void OnSellRequestAttempt(Entity<CETradingPlatformComponent> ent, ref CETradingRequestSellAttempt args)
    {
        if (!TryComp<ItemPlacerComponent>(ent, out var itemPlacer))
            return;

        if (!CanFulfillRequest(ent, args.Request))
            return;

        if (!Proto.TryIndex(args.Request, out var indexedRequest))
            return;

        if (!_economy.TryRerollRequest(args.Faction, args.Request))
            return;

        foreach (var req in indexedRequest.Requirements)
        {
            req.PostCraft(EntityManager, Proto, itemPlacer.PlacedEntities);
        }

        _audio.PlayPvs(ent.Comp.SellSound, Transform(ent).Coordinates);
        var price = GetPrice(indexedRequest) ?? 0;
        _currency.GenerateMoney(price, Transform(ent).Coordinates);
        SpawnAtPosition(ent.Comp.SellVisual, Transform(ent).Coordinates);

        UpdatePlatformUIState(ent);
    }
}
