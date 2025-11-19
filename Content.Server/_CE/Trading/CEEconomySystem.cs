using System.Linq;
using Content.Server.Cargo.Systems;
using Content.Server.GameTicking;
using Content.Server.Station.Events;
using Content.Shared._CE.Trading.BuyServices;
using Content.Shared._CE.Trading.Components;
using Content.Shared._CE.Trading.Prototypes;
using Content.Shared._CE.Trading.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._CE.Trading;

public sealed partial class CEEconomySystem : CESharedEconomySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly PricingSystem _price = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CEStationEconomyComponent, StationPostInitEvent>(OnStationPostInit);
        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs ev)
    {
        if (!ev.WasModified<CETradingPositionPrototype>() && !ev.WasModified<CETradingRequestPrototype>())
            return;

        var query = EntityQueryEnumerator<CEStationEconomyComponent>();
        while (query.MoveNext(out var uid, out var economyComponent))
        {
            UpdatePricing((uid, economyComponent));
            UpdateRequestPricing((uid, economyComponent));
        }
    }

    private void OnStationPostInit(Entity<CEStationEconomyComponent> ent, ref StationPostInitEvent args)
    {
        UpdatePricing(ent);
        UpdateRequestPricing(ent);
        GenerateStartingRequests(ent);
    }

    private void UpdatePricing(Entity<CEStationEconomyComponent> ent)
    {
        ent.Comp.Pricing.Clear();
        foreach (var trade in _proto.EnumeratePrototypes<CETradingPositionPrototype>())
        {
            double price = 0;
            switch (trade.Service)
            {
                case CEBuyItemsService buyItems:
                    var tempEnt = Spawn(buyItems.Product); //we need to correctly rate items through price event to rate melee weapon damage, amount of magic energy, and so on.
                    price += _price.GetPrice(tempEnt) * buyItems.Count;
                    QueueDel(tempEnt);
                    break;
            }

            price += trade.PriceMarkup;

            //Random fluctuation
            price *= 1 + _random.NextFloat(trade.PriceFluctuation);

            ent.Comp.Pricing.TryAdd(trade, (int) price);
        }
        Dirty(ent);
    }

    private void UpdateRequestPricing(Entity<CEStationEconomyComponent> ent)
    {
        ent.Comp.RequestPricing.Clear();

        foreach (var trade in _proto.EnumeratePrototypes<CETradingRequestPrototype>())
        {
            double price = 0;
            foreach (var req in trade.Requirements)
            {
                price += req.GetPrice(EntityManager, _proto);
            }

            price += trade.AdditionalReward;

            ent.Comp.RequestPricing.TryAdd(trade, (int) price);
        }
        Dirty(ent);
    }

    public bool TryRerollRequest(ProtoId<CETradingFactionPrototype> faction,
        ProtoId<CETradingRequestPrototype> request)
    {
        var query = EntityQueryEnumerator<CEStationEconomyComponent>();

        while (query.MoveNext(out var uid, out var economy))
        {
            if (!economy.ActiveRequests.TryGetValue(faction, out var requests))
                continue;

            if (!requests.Contains(request))
                continue;

            requests.Add(GetNextRequest(faction, requests) ?? request);
            requests.Remove(request);

            Dirty(uid, economy);
            return true;
        }

        return false;
    }

    private void GenerateStartingRequests(Entity<CEStationEconomyComponent> ent)
    {
        ent.Comp.ActiveRequests.Clear();

        var allFactions = _proto.EnumeratePrototypes<CETradingFactionPrototype>();
        foreach (var faction in allFactions)
        {
            var requests = new HashSet<ProtoId<CETradingRequestPrototype>>();
            for (int i = 0; i < ent.Comp.MaxRequestCount; i++)
            {
                var nextRequest = GetNextRequest(faction.ID, requests);

                if (nextRequest == null)
                    break; // No more suitable requests

                requests.Add(nextRequest);
            }
            ent.Comp.ActiveRequests.Add(faction, requests);
        }
    }

    private CETradingRequestPrototype? GetNextRequest(ProtoId<CETradingFactionPrototype> faction, HashSet<ProtoId<CETradingRequestPrototype>> existing)
    {
        Dictionary<CETradingRequestPrototype, float> suitableRequestsWeights = new();

        var allRequests = _proto.EnumeratePrototypes<CETradingRequestPrototype>();
        foreach (var request in allRequests)
        {
            var passed = true;

            if (existing.Contains(request))
                passed = false;

            if (!request.PossibleFactions.Contains(faction))
                passed = false;

            var stationTime = _timing.CurTime.Subtract(_gameTicker.RoundStartTimeSpan);

            if (passed && TimeSpan.FromMinutes(request.FromMinutes) > stationTime)
                passed = false;

            if (passed && request.ToMinutes.HasValue && TimeSpan.FromMinutes(request.ToMinutes.Value) < stationTime)
                passed = false;

            if (passed)
                suitableRequestsWeights.Add(request, request.GenerationWeight);
        }

        return RequestPick(suitableRequestsWeights, _random);
    }

    /// <summary>
    /// Optimization moment: avoid re-indexing for weight selection
    /// </summary>
    private static CETradingRequestPrototype? RequestPick(Dictionary<CETradingRequestPrototype, float> weights, IRobustRandom random)
    {
        if (weights.Count == 0)
            return null; // No suitable requests

        var picks = weights;
        var sum = picks.Values.Sum();
        var accumulated = 0f;

        var rand = random.NextFloat() * sum;

        foreach (var (key, weight) in picks)
        {
            accumulated += weight;

            if (accumulated >= rand)
            {
                return key;
            }
        }

        // Shouldn't happen
        throw new InvalidOperationException($"Invalid weighted pick in CEStationEconomySystem!");
    }
}
