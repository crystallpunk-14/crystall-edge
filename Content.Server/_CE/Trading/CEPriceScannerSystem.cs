using Content.Server.Cargo.Systems;
using Content.Shared._CE.Currency;
using Content.Shared._CE.Examine;
using Content.Shared.Inventory;
using Content.Shared.Tag;
using Content.Shared._CE.Trading.Components;
using Content.Shared.Mobs.Components;

namespace Content.Server._CE.Trading;

public sealed partial class CEPriceScannerSystem : EntitySystem
{
    [Dependency] private PricingSystem _price = default!;
    [Dependency] private TagSystem _tag = default!;
    [Dependency] private InventorySystem _invSystem = default!;
    [Dependency] private CESharedCurrencySystem _currency = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<CEExamineAugmentEvent>(OnExamineAugment);
    }

    private bool IsAbleExamine(EntityUid uid)
    {
        if (HasComp<CEPriceScannerComponent>(uid))
            return true;
        if (_invSystem.TryGetSlotEntity(uid, "eyes", out var huds) && HasComp<CEPriceScannerComponent>(huds))
            return true;

        return false;
    }

    private void OnExamineAugment(CEExamineAugmentEvent args)
    {
        if (!IsAbleExamine(args.Examiner))
            return;
        if (_tag.HasTag(args.Examined, CETradingPlatformSystem.CoinTag))
            return;
        if (HasComp<MobStateComponent>(args.Examined))
            return;

        var price = Math.Round(_price.GetPrice(args.Examined));

        if (price <= 0)
            return;

        var priceMsg = Loc.GetString("ce-currency-examine-title");

        priceMsg += _currency.GetCurrencyPrettyString((int)price);

        args.AddMarkup(priceMsg);
    }
}
