using System.Numerics;
using Content.IntegrationTests.Fixtures;
using Content.Server._CE.GOAP.Consumption;
using Content.Server._CE.GOAP.Selectors;
using Content.Shared._CE.EntityConditions.Conditions;
using Content.Shared._CE.GOAP;
using Content.Shared._CE.GOAP.Consumption;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.FixedPoint;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Whitelist;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;

namespace Content.IntegrationTests.Tests._CE;

[TestFixture]
public sealed class CEDirectConsumptionTest : GameTest
{
    [TestPrototypes]
    private const string Prototypes = """
- type: Tag
  id: CEDirectConsumptionTestFood

- type: entity
  id: CEDirectConsumptionTestConsumer
  parent: MobHuman
  components:
  - type: CEGOAP
    startSleeping: true

- type: entity
  id: CEDirectConsumptionTestFood
  components:
  - type: Item
    size: Tiny
  - type: Tag
    tags: [CEDirectConsumptionTestFood]
  - type: CEFoodTag
    tags: [CEWheat]
  - type: Solution
    id: food
    solution:
      maxVol: 20
      reagents:
      - ReagentId: Nutriment
        Quantity: 5
  - type: Edible
    solution: food
    delay: 0.2
    transferAmount: 5
    destroyOnEmpty: false
    utensil: None

- type: entity
  id: CEDirectConsumptionTestDrink
  parent: CEDirectConsumptionTestFood
  components:
  - type: Solution
    id: food
    solution:
      maxVol: 20
      reagents:
      - ReagentId: Water
        Quantity: 5
  - type: Edible
    edible: Drink

- type: entity
  id: CEDirectConsumptionTestContainer
  components:
  - type: ItemSlots
    slots:
      food: {}
  - type: CEOpenContainer
    containers: [food]
""";

    [Test]
    public async Task SelectsActualContainedFoodAndRespectsSlotAccess()
    {
        var map = await Pair.CreateTestMap();
        await Server.WaitAssertion(() =>
        {
            var consumer = SpawnConsumer(map.GridCoords);
            var loose = SpawnFood(map.GridCoords.Offset(new Vector2(2, 0)));
            var contained = SpawnFood(map.GridCoords.Offset(new Vector2(0.5f, 0)));
            var holder = SEntMan.SpawnEntity("CEDirectConsumptionTestContainer", map.GridCoords.Offset(new Vector2(0.5f, 0)));
            var slots = SEntMan.System<ItemSlotsSystem>();
            Assert.That(slots.TryInsert(holder, "food", contained, null), Is.True);

            var selector = new CEGOAPSelectorEdible { Range = 5 };
            Assert.That(selector.Resolve(consumer, SEntMan).Entity, Is.EqualTo(contained));
            slots.SetLock(holder, "food", true);
            Assert.That(selector.Resolve(consumer, SEntMan).Entity, Is.EqualTo(loose));
            slots.SetLock(holder, "food", false);
            Assert.That(selector.Resolve(consumer, SEntMan).Entity, Is.EqualTo(contained));
        });
    }

    [Test]
    public async Task PreferencesDoNotChangeDigestibilityAndDrinksUseCurrentSolution()
    {
        var map = await Pair.CreateTestMap();
        await Server.WaitAssertion(() =>
        {
            var consumer = SpawnConsumer(map.GridCoords);
            var food = SpawnFood(map.GridCoords.Offset(new Vector2(0.5f, 0)));
            var selector = new CEGOAPSelectorEdible { Range = 5, AllowedFoodTags = ["CEWheat"] };
            Assert.That(selector.Resolve(consumer, SEntMan).Entity, Is.EqualTo(food));
            selector.ForbiddenFoodTags.Add("CEWheat");
            Assert.That(selector.Resolve(consumer, SEntMan).Entity, Is.Null);
            Assert.That(SEntMan.System<IngestionSystem>().CanIngest(consumer, food), Is.True);

            var drink = SEntMan.SpawnEntity("CEDirectConsumptionTestDrink", map.GridCoords.Offset(new Vector2(0.5f, 0)));
            var waterSelector = new CEGOAPSelectorEdible
            {
                Range = 5,
                Edible = IngestionSystem.Drink,
                SolutionConditions = [new CEReagentFractionCondition { Reagent = "Water", MinFraction = 0.5f }],
            };
            Assert.That(waterSelector.Resolve(consumer, SEntMan).Entity, Is.EqualTo(drink));
            var solutions = SEntMan.System<SharedSolutionContainerSystem>();
            Assert.That(solutions.TryGetSolution(drink, "food", out var solution), Is.True);
            Assert.That(solutions.TryAddSolution(solution.Value, new Solution("Nutriment", 10)), Is.True);
            Assert.That(waterSelector.Resolve(consumer, SEntMan).Entity, Is.Null);
        });
    }

    [Test]
    public async Task GenericSelectorKeepsStartedTargetWhenCloserFoodAppears()
    {
        var map = await Pair.CreateTestMap();
        EntityUid consumer = default;
        EntityUid original = default;
        EntityUid closer = default;
        var action = ConsumeAction();
        await Server.WaitAssertion(() =>
        {
            consumer = SpawnConsumer(map.GridCoords);
            original = SpawnFood(map.GridCoords.Offset(new Vector2(0.8f, 0)));
            action.RaiseStartup(consumer, SEntMan);
            closer = SpawnFood(map.GridCoords.Offset(new Vector2(0.2f, 0)));
            Assert.That(action.Selector!.Resolve(consumer, SEntMan).Entity, Is.EqualTo(closer));
            Assert.That(action.RaiseUpdate(consumer, 0.1f, SEntMan), Is.EqualTo(CEGOAPActionStatus.Running));
        });

        await Server.WaitRunTicks(30);
        await Server.WaitAssertion(() =>
        {
            Assert.That(Volume(original), Is.EqualTo(FixedPoint2.Zero));
            Assert.That(Volume(closer), Is.EqualTo(FixedPoint2.New(5)));
            Assert.That(action.RaiseUpdate(consumer, 0.1f, SEntMan), Is.EqualTo(CEGOAPActionStatus.Finished));
            action.RaiseShutdown(consumer, SEntMan);
        });
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task CancellationAndComponentShutdownDoNotConsumeFood(bool removeComponent)
    {
        var map = await Pair.CreateTestMap();
        EntityUid food = default;
        await Server.WaitAssertion(() =>
        {
            var consumer = SpawnConsumer(map.GridCoords);
            food = SpawnFood(map.GridCoords.Offset(new Vector2(0.5f, 0)));
            var action = ConsumeAction();
            action.RaiseStartup(consumer, SEntMan);
            Assert.That(action.RaiseUpdate(consumer, 0.1f, SEntMan), Is.EqualTo(CEGOAPActionStatus.Running));
            if (removeComponent)
                SEntMan.RemoveComponent<CEGOAPConsumeComponent>(consumer);
            else
                action.RaiseShutdown(consumer, SEntMan);
        });

        await Server.WaitRunTicks(30);
        await Server.WaitAssertion(() => Assert.That(Volume(food), Is.EqualTo(FixedPoint2.New(5))));
    }

    [Test]
    public async Task ConcurrentConsumersShareOneNativePortionWithoutDuplicateSuccess()
    {
        var map = await Pair.CreateTestMap();
        EntityUid first = default;
        EntityUid second = default;
        EntityUid food = default;
        var action = ConsumeAction();
        await Server.WaitAssertion(() =>
        {
            first = SpawnConsumer(map.GridCoords);
            second = SpawnConsumer(map.GridCoords);
            food = SpawnFood(map.GridCoords.Offset(new Vector2(0.5f, 0)));
            action.RaiseStartup(first, SEntMan);
            action.RaiseStartup(second, SEntMan);
            Assert.That(action.RaiseUpdate(first, 0.1f, SEntMan), Is.EqualTo(CEGOAPActionStatus.Running));
            Assert.That(action.RaiseUpdate(second, 0.1f, SEntMan), Is.EqualTo(CEGOAPActionStatus.Running));
        });

        await Server.WaitRunTicks(30);
        await Server.WaitAssertion(() =>
        {
            var firstStatus = action.RaiseUpdate(first, 0.1f, SEntMan);
            var secondStatus = action.RaiseUpdate(second, 0.1f, SEntMan);
            Assert.That(Volume(food), Is.EqualTo(FixedPoint2.Zero));
            Assert.That(new[] { firstStatus, secondStatus }, Is.EquivalentTo(new[]
            {
                CEGOAPActionStatus.Finished,
                CEGOAPActionStatus.Failed,
            }));
            action.RaiseShutdown(first, SEntMan);
            action.RaiseShutdown(second, SEntMan);
        });
    }

    private EntityUid SpawnConsumer(EntityCoordinates coordinates)
        => SEntMan.SpawnEntity("CEDirectConsumptionTestConsumer", coordinates);

    private EntityUid SpawnFood(EntityCoordinates coordinates)
        => SEntMan.SpawnEntity("CEDirectConsumptionTestFood", coordinates);

    private FixedPoint2 Volume(EntityUid food)
    {
        Assert.That(SEntMan.System<SharedSolutionContainerSystem>().TryGetSolution(food, "food", out _, out var solution), Is.True);
        return solution!.Volume;
    }

    private static CEGOAPConsumeAction ConsumeAction() => new()
    {
        RetryDelay = TimeSpan.FromSeconds(2),
        // Deliberately use a general selector: consuming must not downcast it to an edible selector.
        Selector = new CEGOAPSelectorNearestEntity
        {
            Range = 5,
            Whitelist = new EntityWhitelist { Tags = ["CEDirectConsumptionTestFood"] },
        },
    };
}
