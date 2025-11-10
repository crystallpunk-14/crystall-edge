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
    private void InitializeGather()
    {
        SubscribeLocalEvent<CEPlantComponent, DestructionEventArgs>(OnPlantDestruction);
        SubscribeLocalEvent<CEPlantGatherOnInteractComponent, InteractUsingEvent>(OnGatherableInteract);
        SubscribeLocalEvent<CEPlantGatherOnInteractComponent, CEPlantGatherDoAfterEvent>(OnGatherDoAfter);
    }

    /// <summary>
    /// We gather inner plant resources
    /// </summary>
    private void OnPlantDestruction(Entity<CEPlantComponent> ent, ref DestructionEventArgs args)
    {
        var pos = Transform(ent).Coordinates;

        foreach (var (produceProto, maxCount) in ent.Comp.DestructProduce)
        {
            var produceCount = ContentHelpers.RoundToEqualLevels(ent.Comp.GrowthLevel, 1, maxCount);

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

        if (!CanHarvestPlant((ent, producing), ent.Comp.Gathers))
            return;

        var doAfterArgs =
            new DoAfterArgs(EntityManager,
                args.User,
                ent.Comp.GatherDelay,
                new CEPlantGatherDoAfterEvent(ent.Comp.Gathers),
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
            if (!ent.Comp.Gathers.TryGetValue(gatherType, out var entry))
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
    /// <param name="ent"></param>
    /// <param name="gatherTypes"></param>
    /// <param name="plantComponent"></param>
    /// <param name="result"></param>
    private void HarvestPlant(Entity<CEPlantProducingComponent> ent,
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
            DebugTools.Assert(ent.Comp.Gathers.ContainsKey(gatherType)); //Just for sure

            var entry = ent.Comp.Gathers[gatherType];

            var produceCount = ContentHelpers.RoundToEqualLevels(entry.Growth, 1, entry.MaxProduce);

            if (produceCount == 0)
                continue;

            for (var i = 0; i < produceCount; i++)
            {
                var spawnPos = pos.Offset(_random.NextVector2(ent.Comp.GatherOffset));
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
