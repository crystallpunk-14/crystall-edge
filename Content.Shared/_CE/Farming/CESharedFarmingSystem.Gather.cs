using Content.Shared._CE.Farming.Components;
using Content.Shared.Destructible;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Rounding;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared._CE.Farming;

public abstract partial class CESharedFarmingSystem
{
    /// <summary>
    /// Message to future descendants: There is some crap in this system.
    /// I couldn't implement the resource gathering mechanics nicely, so there is some code duplication here,
    /// but I'll try to explain what's what. Currently, there are two types of resource gathering implemented here.
    ///
    /// 1) CEPlantProducingComponent. This component allows you to grow many different resources on a single plant and,
    /// through various interactions (destruction, interaction with a tool), collect one or more types of grown resources.
    /// On the client side, it is also possible to visualize each type of growing resource.
    ///
    /// 2) CEPlantAdditionalProduceOnDestructComponent and CEPlantAdditionalProduceOnInteractComponent.
    /// There's some crap here. I needed to be able to store the harvested resource,
    /// which is directly linked to the plant's growth level, its GrowthLevel.
    /// Harvesting this resource essentially destroys the plant.
    ///
    /// This was done to make an apple tree! An apple tree can grow apples,
    /// but it also has its own stages of growth.
    /// Depending on the stage of growth, different amounts of wood fall from it.
    /// </summary>
    //
    //              .:'
    //      __ :'__
    //   .'`__`-'__``.
    //  :__________.-'
    //  :_________:
    //   :_________`-;
    //    `.__.-.__.'  apples
    private void InitializeGather()
    {
        SubscribeLocalEvent<CEPlantAdditionalProduceOnDestructComponent, DestructionEventArgs>(OnPlantDestruction);

        SubscribeLocalEvent<CEPlantAdditionalProduceOnInteractComponent, InteractUsingEvent>(OnAdditionalPlantInteract);
        SubscribeLocalEvent<CEPlantAdditionalProduceOnInteractComponent, CEPlantGatherDoAfterEvent>(OnAdditionalProduceDoAfter);

        SubscribeLocalEvent<CEPlantGatherOnInteractComponent, InteractUsingEvent>(OnGatherableInteract);
        SubscribeLocalEvent<CEPlantGatherOnInteractComponent, CEPlantGatherDoAfterEvent>(OnGatherDoAfter);
    }

    private void OnAdditionalPlantInteract(Entity<CEPlantAdditionalProduceOnInteractComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (_whitelist.IsWhitelistFailOrNull(ent.Comp.ToolWhitelist, args.Used))
            return;

        HashSet<EntProtoId> hashSet = new();

        foreach (var (proto, value) in ent.Comp.Produce)
        {
            hashSet.Add(proto);
        }

        var doAfterArgs =
            new DoAfterArgs(EntityManager,
                args.User,
                ent.Comp.GatherDelay,
                new CEPlantGatherDoAfterEvent(hashSet),
                ent,
                used: args.Used)
            {
                BreakOnDamage = true,
                BlockDuplicate = false,
                CancelDuplicate = false,
                BreakOnMove = true,
                BreakOnHandChange = true,
            };

        if (_net.IsServer) //For some reason we have sound spamming here. PlayPredicted dont work, idk why
            _audio.PlayPvs(ent.Comp.GatherSound, Transform(ent).Coordinates);

        args.Handled = _doAfter.TryStartDoAfter(doAfterArgs);
    }

    /// <summary>
    /// We gather inner plant resources
    /// </summary>
    private void OnPlantDestruction(Entity<CEPlantAdditionalProduceOnDestructComponent> ent, ref DestructionEventArgs args)
    {
        if (!PlantQuery.TryComp(ent, out var plant))
            return;

        var pos = Transform(ent).Coordinates;

        foreach (var (produceProto, maxCount) in ent.Comp.Produce)
        {
            var produceCount = ContentHelpers.RoundToLevels(plant.GrowthLevel, 1, maxCount);

            if (produceCount == 0)
                continue;

            for (var i = 0; i < produceCount; i++)
            {
                var spawnPos = pos.Offset(_random.NextVector2(0.3f)); //Boo hardcoding
                PredictedSpawnAtPosition(produceProto, spawnPos);
            }
        }
    }

    private void OnGatherableInteract(Entity<CEPlantGatherOnInteractComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (!PlantProducingQuery.TryComp(ent, out var producing))
            return;

        if (_whitelist.IsWhitelistFailOrNull(ent.Comp.ToolWhitelist, args.Used))
            return;

        if (!CanHarvestPlant((ent, producing), ent.Comp.Produce))
            return;

        var doAfterArgs =
            new DoAfterArgs(EntityManager,
                args.User,
                ent.Comp.GatherDelay,
                new CEPlantGatherDoAfterEvent(ent.Comp.Produce),
                ent,
                used: args.Used)
            {
                BreakOnDamage = true,
                BlockDuplicate = false,
                CancelDuplicate = false,
                BreakOnMove = true,
                BreakOnHandChange = true,
            };

        if (_net.IsServer) //For some reason we have sound spamming here. PlayPredicted dont work, idk why
            _audio.PlayPvs(ent.Comp.GatherSound, Transform(ent).Coordinates);

        args.Handled = _doAfter.TryStartDoAfter(doAfterArgs);
    }

    private void OnGatherDoAfter(Entity<CEPlantGatherOnInteractComponent> ent, ref CEPlantGatherDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        if (!PlantProducingQuery.TryComp(ent, out var producing))
            return;

        if (!_timing.IsFirstTimePredicted)
            return;

        args.Handled = true;

        HarvestPlant((ent, producing), args.GatherTypes, null, out _);
    }

    private void OnAdditionalProduceDoAfter(Entity<CEPlantAdditionalProduceOnInteractComponent> ent, ref CEPlantGatherDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        if (!PlantQuery.TryComp(ent, out var plant))
            return;

        if (!_timing.IsFirstTimePredicted)
            return;

        args.Handled = true;

        var pos = Transform(ent).Coordinates;

        foreach (var (produceProto, maxCount) in ent.Comp.Produce)
        {
            var produceCount = ContentHelpers.RoundToLevels(plant.GrowthLevel, 1, maxCount);

            if (produceCount == 0)
                continue;

            for (var i = 0; i < produceCount; i++)
            {
                var spawnPos = pos.Offset(_random.NextVector2(0.3f)); //Boo hardcoding
                PredictedSpawnAtPosition(produceProto, spawnPos);
            }
        }
        QueueDel(ent);
    }

    /// <summary>
    /// Checks whether it is possible to harvest the plant using any of the specified methods.
    /// </summary>
    private bool CanHarvestPlant(Entity<CEPlantProducingComponent> ent, HashSet<EntProtoId> gatherTypes, CEPlantComponent? plantComponent = null)
    {
        if (!PlantQuery.Resolve(ent, ref plantComponent))
            return false;

        var canHarvest = false;
        foreach (var gatherType in gatherTypes)
        {
            if (!ent.Comp.Produce.TryGetValue(gatherType, out var entry))
                continue;

            var produceCount = ContentHelpers.RoundToEqualLevels(entry.Growth, 1, entry.MaxProduce);

            if (produceCount == 0)
                continue;

            canHarvest = true;
        }

        return canHarvest;
    }

    /// <summary>
    /// We extract all resources of the specified types from the plant.
    /// </summary>
    public void HarvestPlant(Entity<CEPlantProducingComponent> ent,
        HashSet<EntProtoId> gatherTypes,
        CEPlantComponent? plantComponent,
        out HashSet<EntityUid> result)
    {
        result = new();

        if (!Resolve(ent.Owner, ref plantComponent))
            return;

        var pos = Transform(ent).Coordinates;

        foreach (var gatherType in gatherTypes)
        {
            DebugTools.Assert(ent.Comp.Produce.ContainsKey(gatherType)); //Just for sure

            var entry = ent.Comp.Produce[gatherType];

            var produceCount = ContentHelpers.RoundToEqualLevels(entry.Growth, 1, entry.MaxProduce);

            if (produceCount == 0)
                continue;

            for (var i = 0; i < produceCount; i++)
            {
                var spawnPos = pos.Offset(_random.NextVector2(_random.NextFloat(ent.Comp.GatherOffset)));
                result.Add(PredictedSpawnAtPosition(gatherType, spawnPos));
            }

            entry.Growth = 0;
        }
        Dirty(ent);
    }
}

[Serializable, NetSerializable]
public sealed partial class CEPlantGatherDoAfterEvent: DoAfterEvent
{

    public HashSet<EntProtoId> GatherTypes;

    public CEPlantGatherDoAfterEvent(HashSet<EntProtoId> gatherTypes)
    {
        GatherTypes = gatherTypes;
    }

    public override DoAfterEvent Clone() => this;
}
