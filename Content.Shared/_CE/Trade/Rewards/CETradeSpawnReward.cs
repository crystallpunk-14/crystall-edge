using Content.Shared.Hands.EntitySystems;
using Content.Shared.Stacks;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Trade.Rewards;

public sealed partial class CETradeSpawnReward : CETradeReward
{
    [DataField(required: true)]
    public EntProtoId Proto;

    [DataField]
    public int Count = 1;

    public override void Give(IEntityManager entMan, EntityUid buyer, List<EntityUid> given)
    {
        var hands = entMan.System<SharedHandsSystem>();

        for (var i = 0; i < Count; i++)
        {
            var spawned = entMan.SpawnNextToOrDrop(Proto, buyer);
            hands.TryPickupAnyHand(buyer, spawned, checkActionBlocker: false);
            given.Add(spawned);
        }
    }

    public override double GetPrice(IEntityManager entMan)
    {
        return entMan.System<CESharedTradeSystem>().EstimatePrice(Proto) * Count;
    }

    public override string GetName(IPrototypeManager protoMan)
    {
        if (!protoMan.TryIndex(Proto, out var indexed))
            return Proto;

        var count = Count;
        if (indexed.TryGetComponent<StackComponent>(out var stack, IoCManager.Resolve<IComponentFactory>()))
            count *= stack.Count;

        return count > 1 ? $"{indexed.Name} x{count}" : indexed.Name;
    }

    public override EntProtoId? GetPreview()
    {
        return Proto;
    }
}
