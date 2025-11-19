using Content.Shared._CE.Trading.Components;
using Content.Shared._CE.Trading.Prototypes;
using Content.Shared.Interaction.Events;
using Content.Shared.Placeable;
using Content.Shared.Popups;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared._CE.Trading.Systems;

public abstract partial class CESharedTradingPlatformSystem : EntitySystem
{
    [Dependency] private readonly SharedUserInterfaceSystem _userInterface = default!;
    [Dependency] protected readonly IPrototypeManager Proto = default!;
    [Dependency] protected readonly IGameTiming Timing = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();
        InitializeUI();

        SubscribeLocalEvent<CETradingReputationComponent, MapInitEvent>(OnReputationMapInit);
        SubscribeLocalEvent<CETradingContractComponent, UseInHandEvent>(OnContractUse);
    }

    private void OnReputationMapInit(Entity<CETradingReputationComponent> ent, ref MapInitEvent args)
    {
        foreach (var faction in Proto.EnumeratePrototypes<CETradingFactionPrototype>())
        {
            if (faction.RoundStart is not null)
            {
                ent.Comp.Reputation[faction] = ent.Comp.Reputation.GetValueOrDefault(faction, 0f) + faction.RoundStart.Value;
            }
        }
        Dirty(ent);
    }

    private void OnContractUse(Entity<CETradingContractComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;
        if (!Proto.TryIndex(ent.Comp.Faction, out var indexedFaction))
            return;

        args.Handled = true;

        var repComp = EnsureComp<CETradingReputationComponent>(args.User);
        repComp.Reputation.TryAdd(ent.Comp.Faction, 0);
        _audio.PlayLocal(new SoundCollectionSpecifier("CECoinImpact"), args.User, args.User);
        _popup.PopupClient(Loc.GetString("cp14-trading-contract-use", ("name", Loc.GetString(indexedFaction.Name))), args.User, args.User);

        if (_net.IsServer)
            QueueDel(ent);
    }


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

    public bool CanBuyPosition(Entity<CETradingReputationComponent?> user, ProtoId<CETradingPositionPrototype> position)
    {
        if (!Resolve(user.Owner, ref user.Comp, false))
            return false;
        if (!Proto.TryIndex(position, out var indexedPosition))
            return false;

        if (user.Comp.Reputation[indexedPosition.Faction] < indexedPosition.ReputationLevel)
            return false;

        return true;
    }

    public void AddReputation(Entity<CETradingReputationComponent?> user,
        ProtoId<CETradingFactionPrototype> faction, float rep)
    {
        if (!Resolve(user.Owner, ref user.Comp, false))
            return;

        if (!user.Comp.Reputation.ContainsKey(faction))
            user.Comp.Reputation.Add(faction, rep);
        else
            user.Comp.Reputation[faction] += rep;

        Dirty(user);
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
public sealed class CETradingPositionBuyAttempt(ProtoId<CETradingPositionPrototype> position) : BoundUserInterfaceMessage
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
