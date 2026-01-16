using Content.Server.Cargo.Systems;
using Content.Shared._CE.Currency;
using Content.Shared.Examine;
using Content.Shared.Inventory;
using Content.Shared.Tag;
using Content.Shared._CE.Trading.Components;
using Content.Shared.Mobs.Components;

namespace Content.Server._CE.Trading;

public sealed class CEPriceScannerSystem : EntitySystem
{
    [Dependency] private readonly PricingSystem _price = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly InventorySystem _invSystem = default!;
    [Dependency] private readonly CESharedCurrencySystem _currency = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<MetaDataComponent, ExaminedEvent>(OnExamined);
    }

    private bool IsAbleExamine(EntityUid uid)
    {
        if (HasComp<CEPriceScannerComponent>(uid))
            return true;
        if (_invSystem.TryGetSlotEntity(uid, "eyes", out var huds) && HasComp<CEPriceScannerComponent>(huds))
            return true;

        return false;
    }

    private void OnExamined(EntityUid uid, MetaDataComponent component, ExaminedEvent args)
    {
        if (!IsAbleExamine(args.Examiner))
            return;
        if (_tag.HasTag(args.Examined, "CECoin"))
            return;
        if (HasComp<MobStateComponent>(uid))
            return;

        var price = Math.Round(_price.GetPrice(args.Examined));

        if (price <= 0)
            return;

        var priceMsg = Loc.GetString("ce-currency-examine-title");

        priceMsg += _currency.GetCurrencyPrettyString((int)price);

        args.PushMarkup(priceMsg);
    }
}
