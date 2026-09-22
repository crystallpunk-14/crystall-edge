using Content.Shared._CE.Trading.Components;
using Content.Shared._CE.Trading.Prototypes;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Stacks;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Trading.BuyServices;

public sealed partial class CEBuyItemsService : CEStoreBuyService
{
    [DataField(required: true)]
    public EntProtoId Product;

    [DataField]
    public int Count = 1;

    public override void Buy(EntityManager entManager,
        IPrototypeManager prototype,
        Entity<CETradingPlatformComponent> platform,
        EntityUid buyer)
    {
        var physSys = entManager.System<SharedPhysicsSystem>();
        var hands = entManager.System<SharedHandsSystem>();
        var giveToHand = platform.Comp.GiveToBuyerHand;

        for (var i = 0; i < Count; i++)
        {
            var spawned = entManager.SpawnNextToOrDrop(Product, giveToHand ? buyer : platform.Owner);

            if (giveToHand)
                hands.TryPickupAnyHand(buyer, spawned, checkActionBlocker: false, animate: false);

            physSys.WakeBody(spawned);
        }
    }

    public override string GetName(IPrototypeManager protoMan)
    {
        if (!protoMan.TryIndex(Product, out var indexedProduct))
            return ":3";

        var count = Count;
        var factory = IoCManager.Resolve<IComponentFactory>();
        if (indexedProduct.TryGetComponent<StackComponent>(out var stack, factory))
            count *= stack.Count;

        return Count > 0 ? $"{indexedProduct.Name} x{count}" : indexedProduct.Name;
    }

    public override string GetDesc(IPrototypeManager protoMan)
    {
        if (!protoMan.TryIndex(Product, out var indexedProduct))
            return string.Empty;

        return indexedProduct.Description;
    }

    public override EntProtoId GetTexture(IPrototypeManager protoMan)
    {
        return Product;
    }
}
