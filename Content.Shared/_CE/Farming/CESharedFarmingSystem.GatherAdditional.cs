using Content.Shared._CE.Farming.Components;
using Content.Shared.Destructible;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Rounding;

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
    private void InitializeGatherAdditional()
    {
        SubscribeLocalEvent<CEPlantAdditionalProduceOnDestructComponent, BreakageEventArgs>(OnPlantDestruction);

        SubscribeLocalEvent<CEPlantAdditionalProduceOnInteractComponent, InteractUsingEvent>(OnAdditionalPlantInteract);
        SubscribeLocalEvent<CEPlantAdditionalProduceOnInteractComponent, CEPlantGatherDoAfterEvent>(OnAdditionalProduceDoAfter);
    }

    /// <summary>
    /// We gather inner plant resources
    /// </summary>
    private void OnPlantDestruction(Entity<CEPlantAdditionalProduceOnDestructComponent> ent, ref BreakageEventArgs args)
    {
        if (!PlantQuery.TryComp(ent, out var plant))
            return;

        var pos = Transform(ent).Coordinates;

        foreach (var (produceProto, maxCount) in ent.Comp.Produce)
        {
            var produceCount = ContentHelpers.RoundToLevels(plant.GrowthLevel, 1, maxCount + 1);

            if (produceCount == 0)
                continue;

            if (_net.IsServer)
            {
                for (var i = 0; i < produceCount; i++)
                {
                    var spawnPos = pos.Offset(_random.NextVector2(0.3f)); //Boo hardcoding
                    var spawned = SpawnAtPosition(produceProto, spawnPos);
                    _transform.SetLocalRotation(spawned, _random.NextAngle());
                }
            }
        }
    }

    private void OnAdditionalPlantInteract(Entity<CEPlantAdditionalProduceOnInteractComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (_whitelist.IsWhitelistFailOrNull(ent.Comp.ToolWhitelist, args.Used))
            return;

        HashSet<string> hashSet = new();

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
            var produceCount = ContentHelpers.RoundToLevels(plant.GrowthLevel, 1, maxCount + 1);

            if (produceCount == 0)
                continue;

            if (_net.IsServer)
            {
                for (var i = 0; i < produceCount; i++)
                {
                    var spawnPos = pos.Offset(_random.NextVector2(0.3f)); //Boo hardcoding
                    var spawned = SpawnAtPosition(produceProto, spawnPos);
                    _transform.SetLocalRotation(spawned, _random.NextAngle());
                }
            }
        }
        QueueDel(ent);
    }
}
