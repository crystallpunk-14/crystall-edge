using Content.Shared._CE.Trading.Prototypes;
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
        EntityUid platform)
    {
        var physSys = entManager.System<SharedPhysicsSystem>();

        for (var i = 0; i < Count; i++)
        {
            var spawned = entManager.SpawnNextToOrDrop(Product, platform);
            physSys.WakeBody(spawned);
        }
    }

    public override string GetName(IPrototypeManager protoMan)
    {
        if (!protoMan.TryIndex(Product, out var indexedProduct))
            return ":3";

        var count = Count;
        if (indexedProduct.TryGetComponent<StackComponent>(out var stack))
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
