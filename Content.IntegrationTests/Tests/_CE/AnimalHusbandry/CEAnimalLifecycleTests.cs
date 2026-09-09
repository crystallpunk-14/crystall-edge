#nullable enable
using System.Linq;
using System.Numerics;
using Content.IntegrationTests.Fixtures;
using Content.Server._CE.Actions;
using Content.Server._CE.AnimalHusbandry.Lifecycle;
using Content.Server._CE.AnimalHusbandry.Production;
using Content.Server._CE.AnimalHusbandry.Reproduction;
using Content.Shared._CE.Actions;
using Content.Shared._CE.AnimalHusbandry.Reproduction;
using Content.Shared.Actions;
using Content.Shared.Charges.Components;
using Content.Shared.Charges.Systems;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Light.Components;
using Content.Shared.Mind;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Trigger.Components;
using Content.Shared.Trigger.Systems;
using Robust.Shared.Containers;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Map.Components;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.IntegrationTests.Tests._CE.AnimalHusbandry;

/// <summary>Lifecycle ownership across clocks, maps, native containers, and opt-in component serialization.</summary>
[TestFixture]
public sealed class CEAnimalLifecycleTests : GameTest
{
    private static readonly EntProtoId NestPrototype = "CEChickenNestingPerch";

    [TestPrototypes]
    private const string Prototypes = """
- type: entity
  id: CEAnimalGrowthTestHost
  components:
  - type: CEAnimalGrowthHostDeleteTest
  - type: ItemSlots
    slots:
      animal:
        whitelist: { tags: [CEChickenPopulation] }
        insertSound: null
        ejectSound: null

- type: entity
  id: CEAnimalSavableGrowthTest
  parent: CEMobChickenChick
  save: true

- type: entity
  id: CEAnimalSavableProductionTest
  parent: CEMobChickenHenWhite
  save: true
""";

    private void Feed(EntityUid animal)
    {
        var needs = SEntMan.GetComponent<SatiationComponent>(animal);
        var system = Server.System<SatiationSystem>();
        system.SetValue((animal, needs), SatiationSystem.Hunger, 200f);
        system.SetValue((animal, needs), SatiationSystem.Thirst, 400f);
    }

    [Test]
    public async Task GrowthUsesThirtySixHealthyMinutesAcrossMapsAndPauses()
    {
        await Server.WaitAssertion(() =>
        {
            var maps = Server.System<SharedMapSystem>();
            var first = maps.CreateMap(out var firstId);
            var second = maps.CreateMap(out var secondId);
            try
            {
                var chick = SEntMan.SpawnEntity("CEMobChickenChick", new MapCoordinates(Vector2.Zero, firstId));
                Feed(chick);
                var growth = SEntMan.GetComponent<CEAnimalGrowthComponent>(chick);
                var system = Server.System<CEAnimalGrowthSystem>();
                Assert.That(growth.Remaining, Is.EqualTo(TimeSpan.FromSeconds(2160)));
                system.Update(600);
                Assert.That(growth.Remaining, Is.EqualTo(TimeSpan.FromSeconds(1560)));

                // Visual/environmental clock changes do not grant or remove biological age.
                var cycle = SEntMan.EnsureComponent<LightCycleComponent>(first);
                cycle.Duration = TimeSpan.FromSeconds(1);
                cycle.Offset = TimeSpan.FromHours(7);
                SEntMan.RemoveComponent<LightCycleComponent>(first);
                Server.System<SharedTransformSystem>().SetCoordinates(chick, new EntityCoordinates(second, Vector2.Zero));
                Assert.That(growth.Remaining, Is.EqualTo(TimeSpan.FromSeconds(1560)));

                var needs = SEntMan.GetComponent<SatiationComponent>(chick);
                Server.System<SatiationSystem>().SetValue((chick, needs), SatiationSystem.Hunger, 10f);
                system.Update(600);
                Assert.That(growth.Remaining, Is.EqualTo(TimeSpan.FromSeconds(1560)), "Unhealthy time must not count.");
                Feed(chick);
                maps.SetPaused(secondId, true);
                system.Update(600);
                Assert.That(growth.Remaining, Is.EqualTo(TimeSpan.FromSeconds(1560)), "Paused map time must not count.");
                maps.SetPaused(secondId, false);
                system.Update(600);
                Assert.That(growth.Remaining, Is.EqualTo(TimeSpan.FromSeconds(960)));
            }
            finally
            {
                maps.DeleteMap(firstId);
                maps.DeleteMap(secondId);
            }
        });
    }

    [Test]
    public async Task RefusedAdultInsertionPreservesSourceMindAndNeedsThenRetries()
    {
        await Server.WaitAssertion(() =>
        {
            var maps = Server.System<SharedMapSystem>();
            maps.CreateMap(out var mapId);
            EntityUid mindId = default;
            try
            {
                var coordinates = new MapCoordinates(Vector2.Zero, mapId);
                var host = SEntMan.SpawnEntity("CEAnimalGrowthTestHost", coordinates);
                var chick = SEntMan.SpawnEntity("CEMobChickenChickMale", coordinates);
                Feed(chick);
                var slots = Server.System<ItemSlotsSystem>();
                Assert.That(slots.TryInsert(host, "animal", chick, null), Is.True);
                slots.SetLock(host, "animal", true);
                var mind = Server.System<SharedMindSystem>();
                mindId = mind.CreateMind(null).Owner;
                mind.TransferTo(mindId, chick);
                SEntMan.GetComponent<CEAnimalGrowthComponent>(chick).Remaining = TimeSpan.Zero;
                var growth = Server.System<CEAnimalGrowthSystem>();
                growth.Update(1);
                Assert.That(slots.GetItemOrNull(host, "animal"), Is.EqualTo(chick));
                Assert.That(mind.TryGetMind(chick, out var sourceMind, out _), Is.True);
                Assert.That(sourceMind, Is.EqualTo(mindId));

                slots.SetLock(host, "animal", false);
                growth.Update(2);
                var adult = slots.GetItemOrNull(host, "animal");
                Assert.That(adult, Is.Not.Null.And.Not.EqualTo(chick));
                Assert.That(SEntMan.GetComponent<MetaDataComponent>(adult!.Value).EntityPrototype!.ID, Is.EqualTo("CEMobChickenRooster"));
                Assert.That(mind.TryGetMind(adult.Value, out var adultMind, out _), Is.True);
                Assert.That(adultMind, Is.EqualTo(mindId));
                var needs = SEntMan.GetComponent<SatiationComponent>(adult.Value);
                Assert.That(Server.System<SatiationSystem>().GetValueOrNull((adult.Value, needs), SatiationSystem.Hunger), Is.EqualTo(200f));
                Assert.That(Server.System<SatiationSystem>().GetValueOrNull((adult.Value, needs), SatiationSystem.Thirst), Is.EqualTo(400f));
            }
            finally
            {
                if (mindId.IsValid())
                    SEntMan.DeleteEntity(mindId);
                maps.DeleteMap(mapId);
            }
        });
    }

    [Test]
    public async Task GrowthRollbackKeepsSourceAliveWhenInsertionCallbackDeletesHost()
    {
        var maps = Server.System<SharedMapSystem>();
        var mind = Server.System<SharedMindSystem>();
        MapId mapId = default;
        EntityUid chick = default;
        EntityUid mindId = default;
        try
        {
            await Server.WaitAssertion(() =>
            {
                maps.CreateMap(out mapId);
                var coordinates = new MapCoordinates(Vector2.Zero, mapId);
                var host = SEntMan.SpawnEntity("CEAnimalGrowthTestHost", coordinates);
                chick = SEntMan.SpawnEntity("CEMobChickenChickMale", coordinates);
                Feed(chick);
                Assert.That(Server.System<ItemSlotsSystem>().TryInsert(host, "animal", chick, null), Is.True);
                mindId = mind.CreateMind(null).Owner;
                mind.TransferTo(mindId, chick);
                SEntMan.GetComponent<CEAnimalGrowthHostDeleteTestComponent>(host).Armed = true;
                SEntMan.GetComponent<CEAnimalGrowthComponent>(chick).Remaining = TimeSpan.Zero;

                Server.System<CEAnimalGrowthSystem>().Update(1);

                Assert.That(SEntMan.EntityExists(host), Is.False);
                Assert.That(SEntMan.EntityExists(chick), Is.True);
                Assert.That(Server.System<SharedContainerSystem>().IsEntityInContainer(chick), Is.False);
                Assert.That(mind.TryGetMind(chick, out var retainedMind, out _), Is.True);
                Assert.That(retainedMind, Is.EqualTo(mindId));
            });
            await Server.WaitRunTicks(5);
            await Server.WaitAssertion(() =>
            {
                Assert.That(SEntMan.EntityExists(chick), Is.True, "Rollback must not queue the source for deletion.");
                Assert.That(mind.TryGetMind(chick, out var retainedMind, out _), Is.True);
                Assert.That(retainedMind, Is.EqualTo(mindId));
            });
        }
        finally
        {
            await Server.WaitPost(() =>
            {
                if (mindId.IsValid()) SEntMan.DeleteEntity(mindId);
                if (maps.MapExists(mapId)) maps.DeleteMap(mapId);
            });
        }
    }

    [Test]
    public async Task NativeNestInsertionPausesAndResumesIncubationAcrossMaps()
    {
        var maps = Server.System<SharedMapSystem>();
        var slots = Server.System<ItemSlotsSystem>();
        var trigger = Server.System<TriggerSystem>();
        MapId firstId = default;
        MapId secondId = default;
        EntityUid second = default;
        EntityUid nest = default;
        EntityUid egg = default;
        TimeSpan remaining = default;
        try
        {
            await Server.WaitAssertion(() =>
            {
                maps.CreateMap(out firstId);
                second = maps.CreateMap(out secondId);
                var coordinates = new MapCoordinates(Vector2.Zero, firstId);
                nest = SEntMan.SpawnEntity("CEChickenNestingPerch", coordinates);
                egg = SEntMan.SpawnEntity("CEFoodEggChickenFertilized", coordinates);
                Assert.That(SEntMan.HasComponent<ActiveTimerTriggerComponent>(egg), Is.False);
                Assert.That(slots.TryInsert(nest, "egg_0", egg, null), Is.True);
                Assert.That(SEntMan.HasComponent<ActiveTimerTriggerComponent>(egg), Is.True);
            });
            await Server.WaitRunTicks(60);
            await Server.WaitAssertion(() =>
            {
                remaining = trigger.GetRemainingTime(egg)!.Value;
                Assert.That(remaining.TotalSeconds, Is.InRange(358.0, 359.5));
                Assert.That(slots.TryEject(nest, "egg_0", null, out var removed), Is.True);
                Assert.That(removed, Is.EqualTo(egg));
                Assert.That(SEntMan.HasComponent<ActiveTimerTriggerComponent>(egg), Is.False);
            });
            await Server.WaitRunTicks(60);
            await Server.WaitAssertion(() =>
            {
                Assert.That(SEntMan.GetComponent<TimerTriggerComponent>(egg).Delay, Is.EqualTo(remaining));
                Server.System<SharedTransformSystem>().SetCoordinates(nest, new EntityCoordinates(second, Vector2.Zero));
                Server.System<SharedTransformSystem>().SetCoordinates(egg, new EntityCoordinates(second, Vector2.Zero));
                Assert.That(slots.TryInsert(nest, "egg_0", egg, null), Is.True);
                Assert.That(trigger.GetRemainingTime(egg), Is.EqualTo(remaining));
            });
        }
        finally
        {
            await Server.WaitPost(() =>
            {
                if (maps.MapExists(firstId)) maps.DeleteMap(firstId);
                if (maps.MapExists(secondId)) maps.DeleteMap(secondId);
            });
        }
    }

    [Test]
    public async Task InsertionDuringMapPausePreservesTheFullIncubationDelay()
    {
        var maps = Server.System<SharedMapSystem>();
        var trigger = Server.System<TriggerSystem>();
        MapId mapId = default;
        EntityUid nest = default;
        EntityUid egg = default;
        try
        {
            await Server.WaitAssertion(() =>
            {
                maps.CreateMap(out mapId);
                var coordinates = new MapCoordinates(Vector2.Zero, mapId);
                nest = SEntMan.SpawnEntity("CEChickenNestingPerch", coordinates);
                egg = SEntMan.SpawnEntity("CEFoodEggChickenFertilized", coordinates);
                maps.SetPaused(mapId, true);
            });
            await Server.WaitRunTicks(60);
            await Server.WaitAssertion(() =>
            {
                Assert.That(Server.System<ItemSlotsSystem>().TryInsert(nest, "egg_0", egg, null), Is.True);
                Assert.That(SEntMan.HasComponent<ActiveTimerTriggerComponent>(egg), Is.True);
            });
            await Server.WaitRunTicks(60);
            await Server.WaitAssertion(() =>
            {
                maps.SetPaused(mapId, false);
                Assert.That(trigger.GetRemainingTime(egg), Is.EqualTo(TimeSpan.FromSeconds(360)));
            });
        }
        finally
        {
            await Server.WaitPost(() => { if (maps.MapExists(mapId)) maps.DeleteMap(mapId); });
        }
    }

    [Test]
    public async Task RemovingNativeSlotsStopsIncubationAndClearsNestAvailability()
    {
        await Server.WaitAssertion(() =>
        {
            var maps = Server.System<SharedMapSystem>();
            maps.CreateMap(out var mapId);
            try
            {
                var coordinates = new MapCoordinates(Vector2.Zero, mapId);
                var nest = SEntMan.SpawnEntity("CEChickenNestingPerch", coordinates);
                var egg = SEntMan.SpawnEntity("CEFoodEggChickenFertilized", coordinates);
                Assert.That(Server.System<ItemSlotsSystem>().TryInsert(nest, "egg_0", egg, null), Is.True);
                Assert.That(SEntMan.HasComponent<ActiveTimerTriggerComponent>(egg), Is.True);
                SEntMan.RemoveComponent<ItemSlotsComponent>(nest);
                Assert.That(SEntMan.HasComponent<ActiveTimerTriggerComponent>(egg), Is.False);
                Assert.That(SEntMan.HasComponent<CEAnimalNestAvailableComponent>(nest), Is.False);
            }
            finally
            {
                maps.DeleteMap(mapId);
            }
        });
    }

    [Test]
    public async Task EmptyNestAvailabilityTracksNativeSlotRemovalAndRestoration()
    {
        await Server.WaitAssertion(() =>
        {
            var maps = Server.System<SharedMapSystem>();
            maps.CreateMap(out var mapId);
            try
            {
                var nest = SEntMan.SpawnEntity("CEChickenNestingPerch", new MapCoordinates(Vector2.Zero, mapId));
                Assert.That(SEntMan.HasComponent<CEAnimalNestAvailableComponent>(nest), Is.True);
                SEntMan.RemoveComponent<ItemSlotsComponent>(nest);
                Assert.That(SEntMan.HasComponent<CEAnimalNestAvailableComponent>(nest), Is.False);

                SEntMan.AddComponents(nest, SProtoMan.Index(NestPrototype), removeExisting: false);
                Assert.That(SEntMan.GetComponent<ItemSlotsComponent>(nest).Slots.Count, Is.EqualTo(12));
                Assert.That(SEntMan.HasComponent<CEAnimalNestAvailableComponent>(nest), Is.True);

                var slots = Server.System<ItemSlotsSystem>();
                for (var i = 0; i < 12; i++)
                    slots.SetLock(nest, $"egg_{i}", true);
                Assert.That(SEntMan.HasComponent<CEAnimalNestAvailableComponent>(nest), Is.False);
                slots.SetLock(nest, "egg_0", false);
                Assert.That(SEntMan.HasComponent<CEAnimalNestAvailableComponent>(nest), Is.True);
            }
            finally
            {
                maps.DeleteMap(mapId);
            }
        });
    }

    [Test]
    public async Task FullNestDoesNotSpendPendingChargeOrFertility()
    {
        await Server.WaitAssertion(() =>
        {
            var maps = Server.System<SharedMapSystem>();
            maps.CreateMap(out var mapId);
            try
            {
                var coordinates = new MapCoordinates(Vector2.Zero, mapId);
                var nest = SEntMan.SpawnEntity("CEChickenNestingPerch", coordinates);
                var hen = SEntMan.SpawnEntity("CEMobChickenHenWhite", coordinates);
                Feed(hen);
                var slots = Server.System<ItemSlotsSystem>();
                for (var i = 0; i < 12; i++)
                {
                    var egg = SEntMan.SpawnEntity("CEFoodEggChicken", coordinates);
                    Assert.That(slots.TryInsert(nest, $"egg_{i}", egg, null), Is.True);
                }
                var fertility = SEntMan.GetComponent<CEAnimalFertilityComponent>(hen);
                fertility.ProductsRemaining = 2;
                var resolver = Server.System<CEGrantedActionResolverSystem>();
                Assert.That(resolver.TryResolveUnique(hen, "CEActionChickenLayEgg", out var action), Is.True);
                var charges = Server.System<SharedChargesSystem>();
                charges.SetCharges(action.Owner, 1);
                var actions = Server.System<SharedActionsSystem>();
                var request = new RequestPerformActionEvent(SEntMan.GetNetEntity(action), SEntMan.GetNetEntity(nest));
                Assert.That(actions.TryPerformActionChecked(request, hen, allowDoAfter: false, predicted: false, showPopups: false),
                    Is.EqualTo(CEActionExecutionResult.InvalidTarget));
                Assert.That(charges.GetCurrentCharges(action.Owner), Is.EqualTo(1));
                Assert.That(fertility.ProductsRemaining, Is.EqualTo(2));

                Assert.That(slots.TryEject(nest, "egg_0", null, out _), Is.True);
                Assert.That(actions.TryPerformActionChecked(request, hen, allowDoAfter: false, predicted: false, showPopups: false),
                    Is.EqualTo(CEActionExecutionResult.Performed));
                Assert.That(charges.GetCurrentCharges(action.Owner), Is.EqualTo(0));
                Assert.That(fertility.ProductsRemaining, Is.EqualTo(1));
                var laid = slots.GetItemOrNull(nest, "egg_0")!.Value;
                Assert.That(SEntMan.GetComponent<CEAnimalIncubationComponent>(laid).Fertilized, Is.True);
                Assert.That(SEntMan.GetComponent<CEAnimalRoostComponent>(hen).Host, Is.EqualTo(nest));
                Assert.That(SEntMan.GetComponent<CEAnimalNestComponent>(nest).Residents, Does.Contain(hen));
                Assert.That(SEntMan.GetComponent<ItemSlotsComponent>(nest).Slots.Values.Count(slot => slot.HasItem), Is.EqualTo(12));
            }
            finally
            {
                maps.DeleteMap(mapId);
            }
        });
    }

    [Test]
    public async Task PausedAnimalsCountTowardsPopulationOnlyOnTheirCurrentMap()
    {
        await Server.WaitAssertion(() =>
        {
            var maps = Server.System<SharedMapSystem>();
            maps.CreateMap(out var firstId);
            maps.CreateMap(out var secondId);
            try
            {
                var pausedRooster = SEntMan.SpawnEntity("CEMobChickenRooster", new MapCoordinates(Vector2.Zero, firstId));
                Server.System<MetaDataSystem>().SetEntityPaused(pausedRooster, true);

                foreach (var mapId in new[] { firstId, secondId })
                {
                    var coordinates = new MapCoordinates(Vector2.Zero, mapId);
                    var nest = SEntMan.SpawnEntity("CEChickenNestingPerch", coordinates);
                    var hen = SEntMan.SpawnEntity("CEMobChickenHenWhite", coordinates);
                    Feed(hen);
                    SEntMan.GetComponent<CEAnimalProductionComponent>(hen).PopulationLimit = 2;
                    var fertility = SEntMan.GetComponent<CEAnimalFertilityComponent>(hen);
                    fertility.ProductsRemaining = 2;
                    Assert.That(Server.System<CEGrantedActionResolverSystem>().TryResolveUnique(hen, "CEActionChickenLayEgg", out var action), Is.True);
                    Server.System<SharedChargesSystem>().SetCharges(action.Owner, 1);
                    var request = new RequestPerformActionEvent(SEntMan.GetNetEntity(action), SEntMan.GetNetEntity(nest));
                    Assert.That(Server.System<SharedActionsSystem>().TryPerformActionChecked(request, hen,
                        allowDoAfter: false, predicted: false, showPopups: false), Is.EqualTo(CEActionExecutionResult.Performed));

                    var egg = Server.System<ItemSlotsSystem>().GetItemOrNull(nest, "egg_0")!.Value;
                    var fertile = mapId == secondId;
                    Assert.That(SEntMan.GetComponent<CEAnimalIncubationComponent>(egg).Fertilized, Is.EqualTo(fertile));
                    Assert.That(fertility.ProductsRemaining, Is.EqualTo(fertile ? 1 : 2));
                }
            }
            finally
            {
                maps.DeleteMap(firstId);
                maps.DeleteMap(secondId);
            }
        });
    }

    [Test]
    public async Task UnstartedMatingContactRemainsUnsetAfterPause()
    {
        var maps = Server.System<SharedMapSystem>();
        MapId mapId = default;
        EntityUid rooster = default;
        try
        {
            await Server.WaitAssertion(() =>
            {
                maps.CreateMap(out mapId);
                rooster = SEntMan.SpawnEntity("CEMobChickenRooster", new MapCoordinates(Vector2.Zero, mapId));
                var mate = SEntMan.GetComponent<CEAnimalMateComponent>(rooster);
                Assert.That(mate.Cooldown, Is.EqualTo(TimeSpan.FromSeconds(720)));
                mate.InteractionEnd = null;
                Server.System<MetaDataSystem>().SetEntityPaused(rooster, true);
            });
            await Server.WaitRunTicks(60);
            await Server.WaitAssertion(() =>
            {
                Server.System<MetaDataSystem>().SetEntityPaused(rooster, false);
                Assert.That(SEntMan.GetComponent<CEAnimalMateComponent>(rooster).InteractionEnd, Is.Null);
            });
        }
        finally
        {
            await Server.WaitPost(() => { if (maps.MapExists(mapId)) maps.DeleteMap(mapId); });
        }
    }

    /// <summary>
    /// Tests lifecycle component data on opt-in savable fixtures. Production livestock retains
    /// the native BaseSimpleMob policy which excludes mobs from map saves.
    /// </summary>
    [Test]
    public async Task OptInSavablePostInitEntitiesPreserveLifecycleComponentTimes()
    {
        var maps = Server.System<SharedMapSystem>();
        var loader = Server.System<MapLoaderSystem>();
        var timing = Server.ResolveDependency<IGameTiming>();
        var path = new ResPath("/ce-animal-lifecycle-test.yml");
        MapId mapId = default;
        var sourceInitialized = false;
        var sourceLifeStage = EntityLifeStage.PreInit;
        var saveSucceeded = false;
        var loadSucceeded = false;
        TimeSpan[] growthRemaining = [];
        (TimeSpan Production, TimeSpan Poll)[] productionRemaining = [];
        try
        {
            await Server.WaitPost(() =>
            {
                var sourceMap = maps.CreateMap(out mapId, runMapInit: true);
                sourceInitialized = SEntMan.GetComponent<MapComponent>(sourceMap).MapInitialized;
                sourceLifeStage = SEntMan.GetComponent<MetaDataComponent>(sourceMap).EntityLifeStage;
                var coordinates = new MapCoordinates(Vector2.Zero, mapId);
                var chick = SEntMan.SpawnEntity("CEAnimalSavableGrowthTest", coordinates);
                SEntMan.GetComponent<CEAnimalGrowthComponent>(chick).Remaining = TimeSpan.FromSeconds(1234);
                var hen = SEntMan.SpawnEntity("CEAnimalSavableProductionTest", coordinates);
                var production = SEntMan.GetComponent<CEAnimalProductionComponent>(hen);
                production.NextProductionAt = timing.CurTime + TimeSpan.FromSeconds(90);
                production.NextPollAt = timing.CurTime + TimeSpan.FromSeconds(30);
                saveSucceeded = loader.TrySaveMap(mapId, path);
                maps.DeleteMap(mapId);
            });
            Assert.That(sourceInitialized, Is.True);
            Assert.That(sourceLifeStage, Is.EqualTo(EntityLifeStage.MapInitialized));
            Assert.That(saveSucceeded, Is.True);
            await Server.WaitRunTicks(60);
            await Server.WaitPost(() =>
            {
                loadSucceeded = loader.TryLoadMap(path, out var loaded, out _);
                if (!loadSucceeded)
                    return;
                mapId = loaded!.Value.Comp.MapId;
                // Loading does not promise to unpause entities. Inspect saved data on this map
                // independently of activity, while still requiring both original components.
                growthRemaining = SEntMan.EntityQuery<CEAnimalGrowthComponent, TransformComponent>(includePaused: true)
                    .Where(pair => pair.Item2.MapUid == loaded.Value.Owner)
                    .Select(pair => pair.Item1.Remaining)
                    .ToArray();
                productionRemaining = SEntMan.EntityQuery<CEAnimalProductionComponent, TransformComponent>(includePaused: true)
                    .Where(pair => pair.Item2.MapUid == loaded.Value.Owner)
                    .Select(pair => (pair.Item1.NextProductionAt - timing.CurTime, pair.Item1.NextPollAt - timing.CurTime))
                    .ToArray();
            });
            Assert.That(loadSucceeded, Is.True);
            Assert.That(growthRemaining, Has.Length.EqualTo(1), "The savable growth fixture must exist on the loaded map.");
            Assert.That(productionRemaining, Has.Length.EqualTo(1), "The savable production fixture must exist on the loaded map.");
            Assert.That(growthRemaining.Single(), Is.EqualTo(TimeSpan.FromSeconds(1234)));
            Assert.That(productionRemaining.Single().Production, Is.EqualTo(TimeSpan.FromSeconds(90)));
            Assert.That(productionRemaining.Single().Poll, Is.EqualTo(TimeSpan.FromSeconds(30)));
        }
        finally
        {
            await Server.WaitPost(() => { if (maps.MapExists(mapId)) maps.DeleteMap(mapId); });
        }
    }
}

/// <summary>Deletes the host after the source was ejected but before the adult can be inserted.</summary>
[RegisterComponent]
public sealed partial class CEAnimalGrowthHostDeleteTestComponent : Component
{
    public bool Armed;
}

public sealed partial class CEAnimalGrowthHostDeleteTestSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        // The integration-test project does not import the entity subscription generator.
        SubscribeLocalEvent<CEAnimalGrowthHostDeleteTestComponent, ItemSlotInsertAttemptEvent>(OnInserting);
    }

    private void OnInserting(Entity<CEAnimalGrowthHostDeleteTestComponent> ent, ref ItemSlotInsertAttemptEvent args)
    {
        if (!ent.Comp.Armed || args.Slot.HasItem)
            return;
        ent.Comp.Armed = false;
        args.Cancelled = true;
        Del(ent);
    }
}
