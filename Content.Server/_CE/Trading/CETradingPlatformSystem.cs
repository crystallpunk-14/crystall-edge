using Content.Server._CE.Currency;
using Content.Server.Cargo.Systems;
using Content.Shared._CE.Trading;
using Content.Shared._CE.Trading.Components;
using Content.Shared._CE.Trading.Prototypes;
using Content.Shared._CE.Trading.Systems;
using Content.Shared.Mobs.Components;
using Content.Shared.Placeable;
using Content.Shared.Popups;
using Content.Shared.Storage;
using Content.Shared.Storage.Components;
using Content.Shared.Tag;
using Content.Shared.UserInterface;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;

namespace Content.Server._CE.Trading;

public sealed partial class CETradingPlatformSystem : CESharedTradingPlatformSystem
{
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly PricingSystem _price = default!;
    [Dependency] private readonly CECurrencySystem _currency = default!;
    [Dependency] private readonly CEEconomySystem _economy = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly UserInterfaceSystem _userInterface = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CETradingPlatformComponent, CETradingPositionBuyAttempt>(OnBuyAttempt);

        SubscribeLocalEvent<CESellingPlatformComponent, BeforeActivatableUIOpenEvent>(OnBeforeSellingUIOpen);
        SubscribeLocalEvent<CESellingPlatformComponent, ItemPlacedEvent>(OnItemPlaced);
        SubscribeLocalEvent<CESellingPlatformComponent, ItemRemovedEvent>(OnItemRemoved);

        SubscribeLocalEvent<CESellingPlatformComponent, CETradingSellAttempt>(OnSellAttempt);
        SubscribeLocalEvent<CESellingPlatformComponent, CETradingRequestSellAttempt>(OnSellRequestAttempt);
    }

    private void OnSellAttempt(Entity<CESellingPlatformComponent> ent, ref CETradingSellAttempt args)
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
        _currency.GenerateMoney(balance * ent.Comp.PlatformMarkupProcent, Transform(ent).Coordinates);
        SpawnAtPosition(ent.Comp.SellVisual, Transform(ent).Coordinates);

        UpdateSellingUIState(ent);
    }

    private void OnSellRequestAttempt(Entity<CESellingPlatformComponent> ent, ref CETradingRequestSellAttempt args)
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
        var price = GetPrice(indexedRequest) * ent.Comp.PlatformMarkupProcent ?? 0;
        _currency.GenerateMoney(price, Transform(ent).Coordinates);
        SpawnAtPosition(ent.Comp.SellVisual, Transform(ent).Coordinates);

        UpdateSellingUIState(ent);
    }

    private void OnItemRemoved(Entity<CESellingPlatformComponent> ent, ref ItemRemovedEvent args)
    {
        UpdateSellingUIState(ent);
    }

    private void OnItemPlaced(Entity<CESellingPlatformComponent> ent, ref ItemPlacedEvent args)
    {
        UpdateSellingUIState(ent);
    }

    private void OnBuyAttempt(Entity<CETradingPlatformComponent> ent, ref CETradingPositionBuyAttempt args)
    {
        TryBuyPosition(args.Actor, ent, args.Position);
        UpdateTradingUIState(ent, args.Actor);
    }

    private void OnBeforeSellingUIOpen(Entity<CESellingPlatformComponent> ent, ref BeforeActivatableUIOpenEvent args)
    {
        UpdateSellingUIState(ent);
    }

    private void UpdateSellingUIState(Entity<CESellingPlatformComponent> ent)
    {
        if (!TryComp<ItemPlacerComponent>(ent, out var itemPlacer))
            return;

        //Calculate
        double balance = 0;
        foreach (var placed in itemPlacer.PlacedEntities)
        {
            if (!CanSell(placed))
                continue;

            balance += _price.GetPrice(placed);
        }

        _userInterface.SetUiState(ent.Owner, CETradingUiKey.Sell, new CESellingPlatformUiState(GetNetEntity(ent), (int)(balance * ent.Comp.PlatformMarkupProcent)));
    }

    public bool CanSell(EntityUid uid)
    {
        if (_tag.HasTag(uid, "CECoin")) //Boo hardcoding
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

    public bool TryBuyPosition(Entity<CETradingReputationComponent?> user, Entity<CETradingPlatformComponent> platform, ProtoId<CETradingPositionPrototype> position)
    {
        if (Timing.CurTime < platform.Comp.NextBuyTime)
            return false;

        if (!CanBuyPosition(user, position))
            return false;

        if (!Proto.TryIndex(position, out var indexedPosition))
            return false;

        if (!Resolve(user.Owner, ref user.Comp, false))
            return false;

        if (!TryComp<ItemPlacerComponent>(platform, out var itemPlacer))
            return false;

        //Top up balance
        double balance = 0;
        foreach (var placedEntity in itemPlacer.PlacedEntities)
        {
            if (!_tag.HasTag(placedEntity, platform.Comp.CoinTag))
                continue;
            balance += _price.GetPrice(placedEntity);
        }

        var price = GetPrice(position) * platform.Comp.PlatformMarkupProcent ?? 10000;
        if (balance < price)
        {
            // Not enough balance to buy the position
            _popup.PopupEntity(Loc.GetString("ce-trading-failure-popup-money"), platform);
            return false;
        }

        foreach (var placedEntity in itemPlacer.PlacedEntities)
        {
            if (!_tag.HasTag(placedEntity, platform.Comp.CoinTag))
                continue;
            QueueDel(placedEntity);
        }

        balance -= price;

        platform.Comp.NextBuyTime = Timing.CurTime + TimeSpan.FromSeconds(1f);
        Dirty(platform);

        indexedPosition.Service.Buy(EntityManager, Proto, platform);

        _audio.PlayPvs(platform.Comp.BuySound, Transform(platform).Coordinates);

        //return the change
        _currency.GenerateMoney(balance, Transform(platform).Coordinates);
        SpawnAtPosition(platform.Comp.BuyVisual, Transform(platform).Coordinates);
        return true;
    }
}
