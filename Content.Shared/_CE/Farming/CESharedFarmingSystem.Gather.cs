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
        SubscribeLocalEvent<CEPlantGatherOnInteractComponent, InteractUsingEvent>(OnGatherableInteract);
        SubscribeLocalEvent<CEPlantGatherOnInteractComponent, CEPlantGatherDoAfterEvent>(OnGatherDoAfter);
    }

    private void OnGatherableInteract(Entity<CEPlantGatherOnInteractComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (!PlantProducingQuery.TryComp(ent, out var producing))
            return;

        if (_whitelist.IsWhitelistFailOrNull(ent.Comp.ToolWhitelist, args.Used))
            return;

        if (!CanHarvestPlant((ent, producing), ent.Comp.GatherKeys, ent.Comp.GatherAmount))
            return;

        var doAfterArgs =
            new DoAfterArgs(EntityManager,
                args.User,
                ent.Comp.GatherDelay,
                new CEPlantGatherDoAfterEvent(ent.Comp.GatherKeys),
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

        HarvestPlant((ent, producing), args.GatherKeys, null, ent.Comp.GatherAmount, out _);
    }

    /// <summary>
    /// Checks whether it is possible to harvest the plant using any of the specified methods.
    /// </summary>
    private bool CanHarvestPlant(Entity<CEPlantProducingComponent> ent, HashSet<string> gatherKeys, float gatherAmount, CEPlantComponent? plantComponent = null)
    {
        if (!PlantQuery.Resolve(ent, ref plantComponent))
            return false;

        var canHarvest = false;
        foreach (var gatherKey in gatherKeys)
        {
            if (!ent.Comp.GatherKeys.TryGetValue(gatherKey, out var entry))
                continue;

            if (entry.Growth < gatherAmount)
                continue;

            var produceCount = ContentHelpers.RoundToLevels(gatherAmount, 1, entry.MaxProduce + 1);

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
        HashSet<string> gatherKeys,
        CEPlantComponent? plantComponent,
        float gatherAmount,
        out HashSet<EntityUid> result)
    {
        result = new();

        if (!Resolve(ent.Owner, ref plantComponent))
            return;

        var pos = Transform(ent).Coordinates;

        foreach (var gatherKey in gatherKeys)
        {
            DebugTools.Assert(ent.Comp.GatherKeys.ContainsKey(gatherKey)); //Just for sure

            var entry = ent.Comp.GatherKeys[gatherKey];

            if (entry.Growth < gatherAmount)
                continue;

            var produceCount = ContentHelpers.RoundToLevels(gatherAmount, 1, entry.MaxProduce + 1);

            if (produceCount == 0)
                continue;

            if (_net.IsServer)
            {
                for (var i = 0; i < produceCount; i++)
                {
                    var spawnPos = pos.Offset(_random.NextVector2(_random.NextFloat(ent.Comp.GatherOffset)));

                    var spawned = SpawnAtPosition(entry.Result, spawnPos);
                    _transform.SetLocalRotation(spawned, _random.NextAngle());
                    result.Add(spawned);
                }
            }

            entry.Growth -= gatherAmount;
        }
        Dirty(ent);
    }
}

[Serializable, NetSerializable]
public sealed partial class CEPlantGatherDoAfterEvent: DoAfterEvent
{
    public HashSet<string> GatherKeys;

    public CEPlantGatherDoAfterEvent(HashSet<string> gatherKeys)
    {
        GatherKeys = gatherKeys;
    }

    public override DoAfterEvent Clone() => this;
}
