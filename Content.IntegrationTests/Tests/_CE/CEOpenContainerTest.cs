using System.Numerics;
using Content.IntegrationTests.Fixtures;
using Content.Shared._CE.Containers;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Lock;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;

namespace Content.IntegrationTests.Tests._CE;

[TestFixture]
public sealed class CEOpenContainerTest : GameTest
{
    [TestPrototypes]
    private const string Prototypes = @"
- type: entity
  id: CEContainerTestShelf
  components:
  - type: ItemSlots
    slots:
      first:
        swap: false
        ejectOnRemove: true
        insertSound: null
        ejectSound: null
      second:
        swap: false
        ejectOnRemove: true
        insertSound: null
        ejectSound: null
  - type: CEOpenContainer
    containers: [first, second]
  - type: CEContainerSlotVisuals
    slots:
      first: { offset: '-0.25,0.125', rotation: 30 }
      second: { offset: '0.25,-0.125', rotation: 60 }

- type: entity
  id: CEContainerTestStorage
  components:
  - type: Storage
    grid: ['0,0,2,2']
  - type: CEOpenContainer
    containers: [storagebase]

- type: entity
  id: CEContainerTestItem
  components:
  - type: Item
  - type: Appearance

- type: entity
  id: CEContainerTestActor
  components:
  - type: Hands
    hands:
      hand:
        location: Middle
    sortedHands: [hand]
  - type: CEContainerTransferTest
";

    [Test]
    public async Task OpenContentsRespectSlotLocksAndNestedHosts()
    {
        var map = await Pair.CreateTestMap();
        await Server.WaitAssertion(() =>
        {
            var containers = Server.System<SharedContainerSystem>();
            var slots = Server.System<ItemSlotsSystem>();
            var access = Server.System<CEOpenContainerSystem>();
            var interaction = Server.System<SharedInteractionSystem>();
            var actor = SEntMan.SpawnEntity(null, map.GridCoords);
            var shelf = SEntMan.SpawnEntity("CEContainerTestShelf", map.GridCoords);
            var item = SEntMan.SpawnEntity("CEContainerTestItem", map.GridCoords);
            Assert.That(slots.TryGetSlot(shelf, "first", out var first), Is.True);
            Assert.That(slots.TryInsert(shelf, first!, item, null), Is.True);
            Assert.That(interaction.IsAccessible(actor, item), Is.True);

            SEntMan.AddComponent<LockComponent>(shelf);
            Assert.That(interaction.IsAccessible(actor, item), Is.False);
            Server.System<LockSystem>().Unlock(shelf, null);
            Assert.That(interaction.IsAccessible(actor, item), Is.True);

            slots.SetLock((shelf, SEntMan.GetComponent<ItemSlotsComponent>(shelf)), first!, true);
            Assert.That(access.CanAccessContents(actor, first!.ContainerSlot!), Is.False);
            Assert.That(interaction.IsAccessible(actor, item), Is.False);
            slots.SetLock((shelf, SEntMan.GetComponent<ItemSlotsComponent>(shelf)), first!, false);

            var outer = SEntMan.SpawnEntity(null, map.GridCoords);
            var outerContainer = containers.EnsureContainer<Container>(outer, "outer");
            Assert.That(containers.Insert(shelf, outerContainer), Is.True);
            Assert.That(access.CanAccessContents(actor, first.ContainerSlot!), Is.False);
            Assert.That(interaction.IsAccessible(actor, item), Is.False);
            Assert.That(containers.Remove(shelf, outerContainer), Is.True);
            Assert.That(interaction.IsAccessible(actor, item), Is.True);

            // The policy also supports ordinary Storage, with no shelf layout or husbandry component.
            var storage = SEntMan.SpawnEntity("CEContainerTestStorage", map.GridCoords);
            var stored = SEntMan.SpawnEntity("CEContainerTestItem", map.GridCoords);
            Assert.That(containers.TryGetContainer(storage, "storagebase", out var storageContainer), Is.True);
            Assert.That(containers.Insert(stored, storageContainer!), Is.True);
            Assert.That(interaction.IsAccessible(actor, stored), Is.True);
            SEntMan.RemoveComponent<CEOpenContainerComponent>(storage);
            Assert.That(interaction.IsAccessible(actor, stored), Is.False);
        });
        await Pair.RunTicksSync(5);
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task RemovingShelfOrSlotOwnerPreservesItems(bool removeComponent)
    {
        var map = await Pair.CreateTestMap();
        await Server.WaitAssertion(() =>
        {
            var containers = Server.System<SharedContainerSystem>();
            var slots = Server.System<ItemSlotsSystem>();
            var appearance = Server.System<SharedAppearanceSystem>();
            var shelf = SEntMan.SpawnEntity("CEContainerTestShelf", map.GridCoords);
            var firstItem = SEntMan.SpawnEntity("CEContainerTestItem", map.GridCoords);
            var secondItem = SEntMan.SpawnEntity("CEContainerTestItem", map.GridCoords);
            Assert.That(slots.TryInsert((shelf, null), "first", firstItem, null), Is.True);
            Assert.That(slots.TryInsert((shelf, null), "second", secondItem, null), Is.True);
            Assert.That(appearance.TryGetData<Vector2>(secondItem, CEContainerSlotVisuals.Offset, out var offset), Is.True);
            Assert.That(offset, Is.EqualTo(new Vector2(0.25f, -0.125f)));
            Assert.That(SEntMan.GetComponent<TransformComponent>(secondItem).LocalPosition, Is.EqualTo(Vector2.Zero));

            Assert.That(slots.TryGetSlot(shelf, "first", out var first), Is.True);
            Assert.That(containers.Remove(firstItem, first!.ContainerSlot!), Is.True);
            Assert.That(appearance.TryGetData<Vector2>(secondItem, CEContainerSlotVisuals.Offset, out offset), Is.True);
            Assert.That(offset, Is.EqualTo(new Vector2(0.25f, -0.125f)), "Removing another item must not reassign this slot.");

            if (removeComponent)
                SEntMan.RemoveComponent<ItemSlotsComponent>(shelf);
            else
                SEntMan.DeleteEntity(shelf);

            Assert.That(SEntMan.EntityExists(secondItem), Is.True);
            Assert.That(containers.IsEntityInContainer(secondItem), Is.False);
            Assert.That(appearance.TryGetData<bool>(secondItem, CEContainerSlotVisuals.Active, out var active), Is.True);
            Assert.That(active, Is.False);
        });
        await Pair.RunTicksSync(5);
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task RejectedTransferReportsFailureAndDoesNotLoseHeldItem(bool blockRestoration)
    {
        var map = await Pair.CreateTestMap();
        await Server.WaitAssertion(() =>
        {
            var hands = Server.System<SharedHandsSystem>();
            var slots = Server.System<ItemSlotsSystem>();
            var containers = Server.System<SharedContainerSystem>();
            var actor = SEntMan.SpawnEntity("CEContainerTestActor", map.GridCoords);
            var shelf = SEntMan.SpawnEntity("CEContainerTestShelf", map.GridCoords);
            var item = SEntMan.SpawnEntity("CEContainerTestItem", map.GridCoords);
            Assert.That(slots.TryInsertEmpty(shelf, item, actor), Is.False, "A player transfer requires a held item.");
            Assert.That(hands.TryPickup(actor, item, "hand"), Is.True);
            var interference = SEntMan.GetComponent<CEContainerTransferTestComponent>(actor);
            interference.Destination = shelf;
            interference.BlockRestoration = blockRestoration;
            interference.Armed = true;

            Assert.That(slots.TryGetSlot(shelf, "first", out var first), Is.True);
            Assert.That(slots.TryInsertFromHand(shelf, first!, actor), Is.False);
            Assert.That(SEntMan.EntityExists(item), Is.True);
            Assert.That(first!.Item, Is.Not.Null);
            Assert.That(first.Item, Is.Not.EqualTo(item));
            Assert.That(hands.IsHolding(actor, item), Is.EqualTo(!blockRestoration));
            Assert.That(containers.IsEntityInContainer(item), Is.EqualTo(!blockRestoration));
        });
        await Pair.RunTicksSync(5);
    }
}

/// <summary>Test-only callback interference between the hand precheck and actual insertion.</summary>
[RegisterComponent]
public sealed partial class CEContainerTransferTestComponent : Component
{
    public EntityUid Destination;
    public bool Armed;
    public bool BlockRestoration;
    public bool Dropped;
}

public sealed partial class CEContainerTransferTestSystem : EntitySystem
{
    [Dependency] private SharedContainerSystem _containers = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CEContainerTransferTestComponent, EntRemovedFromContainerMessage>(OnRemoved);
        SubscribeLocalEvent<CEContainerTransferTestComponent, ContainerIsInsertingAttemptEvent>(OnInserting);
    }

    private void OnRemoved(Entity<CEContainerTransferTestComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (!ent.Comp.Armed)
            return;
        ent.Comp.Armed = false;
        ent.Comp.Dropped = true;
        var blocker = SpawnAtPosition("CEContainerTestItem", Transform(ent.Comp.Destination).Coordinates);
        _containers.TryGetContainer(ent.Comp.Destination, "first", out var destination);
        _containers.Insert(blocker, destination!);
    }

    private void OnInserting(Entity<CEContainerTransferTestComponent> ent, ref ContainerIsInsertingAttemptEvent args)
    {
        if (ent.Comp.Dropped && ent.Comp.BlockRestoration)
            args.Cancel();
    }
}
