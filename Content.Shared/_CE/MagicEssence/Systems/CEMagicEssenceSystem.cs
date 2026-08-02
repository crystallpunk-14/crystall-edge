using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using Content.Shared._CE.Examine;
using Content.Shared._CE.MagicEssence.Components;
using Content.Shared._CE.MagicEssence.Events;
using Content.Shared._CE.MagicEssence.Prototypes;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Inventory;
using Content.Shared.Stacks;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared._CE.MagicEssence.Systems;

public sealed partial class CEMagicEssenceSystem : EntitySystem
{
    [Dependency] private INetManager _net = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private SharedSolutionContainerSystem _solution = default!;

    private static readonly EntProtoId SingletonProtoId = "CEMagicEssenceSingleton";

    private Dictionary<ProtoId<ReagentPrototype>, ProtoId<CEMagicEssenceTypePrototype>>? _reagentToEssence;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MetaDataComponent, CEMagicEssenceCalculationEvent>(OnMetaDataEssenceCalculation);
        SubscribeLocalEvent<SolutionComponent, CEMagicEssenceCalculationEvent>(OnSolutionEssenceCalculation);
        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);

        SubscribeLocalEvent<CEExamineAugmentEvent>(OnExamineAugment);
        SubscribeLocalEvent<CEMagicEssenceScannerComponent, CEMagicEssenceScanEvent>(OnScanAttempt);
        SubscribeLocalEvent<CEMagicEssenceScannerComponent, InventoryRelayedEvent<CEMagicEssenceScanEvent>>(
            (uid, comp, ev) => OnScanAttempt(uid, comp, ev.Args));
    }

    private void OnScanAttempt(EntityUid uid, CEMagicEssenceScannerComponent component, CEMagicEssenceScanEvent args)
    {
        args.CanScan = true;
    }

    /// <summary>
    /// Shows essence composition on examine while wearing thaumaturgy glasses. Useful for
    /// inspecting items inside storage UIs, where the cursor can't be hovered over them.
    /// </summary>
    private void OnExamineAugment(CEExamineAugmentEvent args)
    {
        var scanEvent = new CEMagicEssenceScanEvent();
        RaiseLocalEvent(args.Examiner, scanEvent);

        if (!scanEvent.CanScan)
            return;

        var essences = GetEssence(args.Examined, includeContents: false);
        if (essences.Count == 0)
            return;

        essences.Sort((a, b) =>
        {
            var byAmount = b.Amount.CompareTo(a.Amount);
            return byAmount != 0 ? byAmount : string.CompareOrdinal(a.Type.Id, b.Type.Id);
        });

        var sb = new StringBuilder();
        sb.Append(Loc.GetString("ce-magic-essence-examine-title"));
        sb.Append('\n');

        foreach (var (type, amount) in essences)
        {
            if (!_proto.Resolve(type, out var essenceProto))
                continue;

            sb.Append($"[color={essenceProto.Color.ToHex()}]{essenceProto.Name}[/color]: x{amount}\n");
        }

        args.AddMarkup(sb.ToString().TrimEnd());
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        _reagentToEssence = null;
        _essenceRecipes = null;
    }

    /// <summary>
    /// Calculates the thaumaturgical essence composition of an entity, including its stack count
    /// and (recursively) the essences of anything held in its containers.
    /// </summary>
    public List<(ProtoId<CEMagicEssenceTypePrototype> Type, int Amount)> GetEssence(EntityUid ent, bool includeContents = true)
    {
        var ev = new CEMagicEssenceCalculationEvent();
        RaiseLocalEvent(ent, ref ev);

        var totals = new Dictionary<ProtoId<CEMagicEssenceTypePrototype>, int>();

        var multiplier = TryComp<StackComponent>(ent, out var stack) ? stack.Count : 1;
        foreach (var (type, amount) in ev.Essences)
        {
            totals[type] = totals.GetValueOrDefault(type) + amount * multiplier;
        }

        if (includeContents && TryComp<ContainerManagerComponent>(ent, out var containers))
        {
            foreach (var container in containers.Containers.Values)
            {
                foreach (var contained in container.ContainedEntities)
                {
                    foreach (var (type, amount) in GetEssence(contained))
                    {
                        totals[type] = totals.GetValueOrDefault(type) + amount;
                    }
                }
            }
        }

        var result = new List<(ProtoId<CEMagicEssenceTypePrototype>, int)>();
        foreach (var (type, amount) in totals)
        {
            if (amount <= 0)
                continue;

            result.Add((type, amount));
        }

        return result;
    }

    /// <summary>
    /// Rolls (and caches, round-wide) the essence composition of an <see cref="CEMagicEssenceStructureComponent"/>-bearing
    /// entity, keyed by its <see cref="EntProtoId"/> so every instance of the same prototype shares one roll.
    /// </summary>
    private void OnMetaDataEssenceCalculation(Entity<MetaDataComponent> ent, ref CEMagicEssenceCalculationEvent args)
    {
        if (args.Handled)
            return;

        if (ent.Comp.EntityPrototype is not { } prototype)
            return;

        EntProtoId protoId = prototype;
        var singleton = EnsureSingleton();

        if (singleton is { } cached && cached.Comp.Cache.TryGetValue(protoId, out var knownEssences))
        {
            foreach (var (type, amount) in knownEssences)
            {
                args.Essences[type] = amount;
            }

            return;
        }

        // Only the server rolls new essence compositions; the client waits for it to sync.
        if (!_net.IsServer || singleton is not { } singletonEnt)
            return;

        if (!TryComp<CEMagicEssenceStructureComponent>(ent.Owner, out var structure))
            return;

        var rolled = new Dictionary<ProtoId<CEMagicEssenceTypePrototype>, int>();
        foreach (var (type, minMax) in structure.Essences)
        {
            var amount = minMax.Next(_random);
            if (amount != 0)
                rolled[type] = amount;
        }

        singletonEnt.Comp.Cache[protoId] = rolled;
        Dirty(singletonEnt);

        foreach (var (type, amount) in rolled)
        {
            args.Essences[type] = amount;
        }
    }

    /// <summary>
    /// Adds essence content from any solution reagent that is the pure liquid embodiment of an essence
    /// type (<see cref="CEMagicEssenceTypePrototype.Reagent"/>). 1 unit of such a reagent = 1 essence.
    /// </summary>
    private void OnSolutionEssenceCalculation(Entity<SolutionComponent> ent, ref CEMagicEssenceCalculationEvent args)
    {
        if (args.Handled)
            return;

        var map = GetReagentEssenceMap();

        foreach (var (_, soln) in _solution.EnumerateSolutions(ent.Owner))
        {
            foreach (var (reagent, quantity) in soln.Comp.Solution.Contents)
            {
                if (!map.TryGetValue(reagent.Prototype, out var essenceType))
                    continue;

                args.Essences[essenceType] = args.Essences.GetValueOrDefault(essenceType) + quantity.Int();
            }
        }
    }

    /// <summary>
    /// Resolves the floating essence entity that a reagent evaporates into, if that reagent is the
    /// pure liquid embodiment of some <see cref="CEMagicEssenceTypePrototype"/> (i.e. has a
    /// <see cref="CEMagicEssenceTypePrototype.EssenceProto"/>). False for any other reagent.
    /// </summary>
    public bool TryGetEssenceProtoForReagent(ProtoId<ReagentPrototype> reagent, out EntProtoId essenceProto)
    {
        essenceProto = default;

        if (!GetReagentEssenceMap().TryGetValue(reagent, out var essenceTypeId))
            return false;

        if (!_proto.TryIndex(essenceTypeId, out var essenceType) || essenceType.EssenceProto is not { } proto)
            return false;

        essenceProto = proto;
        return true;
    }

    private Dictionary<ProtoId<ReagentPrototype>, ProtoId<CEMagicEssenceTypePrototype>> GetReagentEssenceMap()
    {
        if (_reagentToEssence is { } cached)
            return cached;

        var map = new Dictionary<ProtoId<ReagentPrototype>, ProtoId<CEMagicEssenceTypePrototype>>();
        foreach (var essenceType in _proto.EnumeratePrototypes<CEMagicEssenceTypePrototype>())
        {
            if (essenceType.Reagent is { } reagent)
                map[reagent] = essenceType.ID;
        }

        _reagentToEssence = map;
        return map;
    }

    /// <summary>
    /// Picks a random essence type, weighted towards lower tiers (weight = 1 / (tier + 1)) so
    /// primal aspects come up more often than complex ones.
    /// </summary>
    public ProtoId<CEMagicEssenceTypePrototype> GetRandomEssenceType()
    {
        var essences = _proto.EnumeratePrototypes<CEMagicEssenceTypePrototype>().ToList();
        if (essences.Count == 0)
            throw new InvalidOperationException("No CEMagicEssenceTypePrototype prototypes are registered.");

        var totalWeight = 0f;
        foreach (var essence in essences)
            totalWeight += 1f / (essence.Tier + 1);

        var roll = _random.NextFloat() * totalWeight;
        foreach (var essence in essences)
        {
            var weight = 1f / (essence.Tier + 1);
            if (roll < weight)
                return essence.ID;

            roll -= weight;
        }

        return essences[^1].ID;
    }

    private Entity<CEMagicEssenceSingletonComponent>? EnsureSingleton()
    {
        var query = EntityQueryEnumerator<CEMagicEssenceSingletonComponent>();
        if (query.MoveNext(out var uid, out var comp))
            return (uid, comp);

        if (!_net.IsServer)
            return null;

        var newUid = Spawn(SingletonProtoId, MapCoordinates.Nullspace);
        return (newUid, Comp<CEMagicEssenceSingletonComponent>(newUid));
    }
}
