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

    public static readonly ProtoId<TagPrototype> CoinTag = "CECoin";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CETradingPlatformComponent, BeforeActivatableUIOpenEvent>(OnBeforeActivatableUIOpen);

        SubscribeLocalEvent<CETradingPlatformComponent, ItemPlacedEvent>(OnItemPlaced);
        SubscribeLocalEvent<CETradingPlatformComponent, ItemRemovedEvent>(OnItemRemoved);

        SubscribeLocalEvent<CETradingPlatformComponent, CETradingBuyAttempt>(OnBuyAttempt);
        SubscribeLocalEvent<CETradingPlatformComponent, CETradingSellAttempt>(OnSellAttempt);
        SubscribeLocalEvent<CETradingPlatformComponent, CETradingRequestSellAttempt>(OnSellRequestAttempt);
    }

    private void OnItemPlaced(Entity<CETradingPlatformComponent> ent, ref ItemPlacedEvent args)
    {
        UpdatePlatformUIState(ent);
    }

    private void OnItemRemoved(Entity<CETradingPlatformComponent> ent, ref ItemRemovedEvent args)
    {
        UpdatePlatformUIState(ent);
    }

    private void OnBeforeActivatableUIOpen(Entity<CETradingPlatformComponent> ent, ref BeforeActivatableUIOpenEvent args)
    {
        UpdatePlatformUIState(ent);
    }

    private void UpdatePlatformUIState(Entity<CETradingPlatformComponent> ent)
    {
        if (!TryComp<ItemPlacerComponent>(ent, out var itemPlacer))
            return;

        // Calculate sell balance
        double sellBalance = 0;
        foreach (var placed in itemPlacer.PlacedEntities)
        {
            if (!CanSell(placed))
                continue;

            sellBalance += _price.GetPrice(placed);
        }

        // Calculate buy balance
        double buyBalance = 0;
        foreach (var placed in itemPlacer.PlacedEntities)
        {
            if (!_tag.HasTag(placed, ent.Comp.CoinTag))
                continue;

            buyBalance += _price.GetPrice(placed);
        }

        var faction = ent.Comp.Faction;
        _userInterface.SetUiState(ent.Owner, CETradingUiKey.Buy, new CETradingPlatformUiState(GetNetEntity(ent), (int)buyBalance, (int)sellBalance, faction));
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

    private void OnBuyAttempt(Entity<CETradingPlatformComponent> ent, ref CETradingBuyAttempt args)
    {
        if (Timing.CurTime < ent.Comp.NextBuyTime)
            return;

        if (!Proto.TryIndex(args.Position, out var indexedPosition))
            return;

        // Ensure the platform is for the same faction as the position being bought
        if (ent.Comp.Faction != indexedPosition.Faction)
            return;

        if (!TryComp<ItemPlacerComponent>(ent, out var itemPlacer))
            return;

        //Top up balance
        double balance = 0;
        foreach (var placedEntity in itemPlacer.PlacedEntities)
        {
            if (!_tag.HasTag(placedEntity, ent.Comp.CoinTag))
                continue;
            balance += _price.GetPrice(placedEntity);
        }

        var price = GetPrice(args.Position) ?? 10000;
        if (balance < price)
        {
            // Not enough balance to buy the position
            return;
        }

        foreach (var placedEntity in itemPlacer.PlacedEntities)
        {
            if (!_tag.HasTag(placedEntity, ent.Comp.CoinTag))
                continue;
            QueueDel(placedEntity);
        }

        balance -= price;

        ent.Comp.NextBuyTime = Timing.CurTime + TimeSpan.FromSeconds(1f);
        Dirty(ent);

        indexedPosition.Service.Buy(EntityManager, Proto, ent);

        _audio.PlayPvs(ent.Comp.BuySound, Transform(ent).Coordinates);

        //return the change
        _currency.GenerateMoney(balance, Transform(ent).Coordinates);
        SpawnAtPosition(ent.Comp.BuyVisual, Transform(ent).Coordinates);

        UpdatePlatformUIState(ent);
    }

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
