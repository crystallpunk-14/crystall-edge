using Content.Shared._CE.Trading.Components;
using Content.Shared._CE.Trading.Prototypes;
using Content.Shared.Placeable;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared._CE.Trading.Systems;

public abstract partial class CESharedTradingPlatformSystem : EntitySystem
{
    [Dependency] protected readonly IPrototypeManager Proto = default!;
    [Dependency] protected readonly IGameTiming Timing = default!;

    public int? GetPrice(ProtoId<CETradingPositionPrototype> position)
    {
        var query = EntityQueryEnumerator<CEStationEconomyComponent>();

        while (query.MoveNext(out var uid, out var economy))
        {
            if (!economy.Pricing.TryGetValue(position, out var price))
                return null;

            return price;
        }

        return null;
    }

    public int? GetPrice(ProtoId<CETradingRequestPrototype> request)
    {
        var query = EntityQueryEnumerator<CEStationEconomyComponent>();

        while (query.MoveNext(out var uid, out var economy))
        {
            if (!economy.RequestPricing.TryGetValue(request, out var price))
                return null;

            return price;
        }

        return null;
    }

    public HashSet<ProtoId<CETradingRequestPrototype>> GetRequests(ProtoId<CETradingFactionPrototype> faction)
    {
        var query = EntityQueryEnumerator<CEStationEconomyComponent>();

        while (query.MoveNext(out var uid, out var economy))
        {
            if (!economy.ActiveRequests.TryGetValue(faction, out var requests))
                continue;

            return requests;
        }

        return [];
    }

    public bool CanFulfillRequest(EntityUid platform, ProtoId<CETradingRequestPrototype> request)
    {
        if (!TryComp<ItemPlacerComponent>(platform, out var itemPlacer))
            return false;

        if (!Proto.TryIndex(request, out var indexedRequest))
            return false;

        foreach (var requirement in indexedRequest.Requirements)
        {
            if (!requirement.CheckRequirement(EntityManager, Proto, itemPlacer.PlacedEntities))
                return false;
        }

        return true;
    }
}

[Serializable, NetSerializable]
public sealed class CETradingBuyAttempt(ProtoId<CETradingPositionPrototype> position) : BoundUserInterfaceMessage
{
    public readonly ProtoId<CETradingPositionPrototype> Position = position;
}

[Serializable, NetSerializable]
public sealed class CETradingRequestSellAttempt(ProtoId<CETradingRequestPrototype> request, ProtoId<CETradingFactionPrototype> faction) : BoundUserInterfaceMessage
{
    public readonly ProtoId<CETradingRequestPrototype> Request = request;
    public readonly ProtoId<CETradingFactionPrototype> Faction = faction;
}


[Serializable, NetSerializable]
public sealed class CETradingSellAttempt : BoundUserInterfaceMessage
{
}
