using System.Numerics;
using Content.IntegrationTests.Fixtures;
using Content.Server._CE.Containers;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Hands.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;

namespace Content.IntegrationTests.Tests._CE;

[TestFixture]
public sealed class CEConnectedItemSlotsTest : GameTest
{
    [TestPrototypes]
    private const string Prototypes = """
- type: entity
  id: CEConnectedTestShelf
  components:
  - type: ItemSlots
    slots:
      first: { swap: false, insertSound: null, ejectSound: null }
      second: { swap: false, insertSound: null, ejectSound: null }
  - type: CEConnectedItemSlots
    group: shelf
    node: storage
  - type: NodeContainer
    nodes:
      storage:
        !type:AdjacentNode
        nodeGroupID: Default
- type: entity
  id: CEConnectedTestItem
  components:
  - type: Item
- type: entity
  id: CEConnectedTestActor
  components:
  - type: Hands
    hands:
      hand:
        location: Middle
    sortedHands: [hand]
""";

    [Test]
    public async Task ConnectedInsertionUsesCardinalNeighboursAndNativeSlots()
    {
        var map = await Pair.CreateTestMap();
        EntityUid origin = default;
        EntityUid north = default;
        EntityUid east = default;
        EntityUid actor = default;
        await Server.WaitAssertion(() =>
        {
            var maps = Server.System<SharedMapSystem>();
            maps.SetTile(map.Grid.Owner, map.Grid.Comp, new Vector2i(0, 1), map.Tile.Tile);
            maps.SetTile(map.Grid.Owner, map.Grid.Comp, new Vector2i(1, 0), map.Tile.Tile);
            origin = SEntMan.SpawnEntity("CEConnectedTestShelf", map.GridCoords);
            north = SEntMan.SpawnEntity("CEConnectedTestShelf", map.GridCoords.Offset(new Vector2(0, 1)));
            east = SEntMan.SpawnEntity("CEConnectedTestShelf", map.GridCoords.Offset(new Vector2(1, 0)));
            actor = SEntMan.SpawnEntity("CEConnectedTestActor", map.GridCoords);
            var transform = Server.System<SharedTransformSystem>();
            transform.AnchorEntity(origin, SEntMan.GetComponent<TransformComponent>(origin));
            transform.AnchorEntity(north, SEntMan.GetComponent<TransformComponent>(north));
            transform.AnchorEntity(east, SEntMan.GetComponent<TransformComponent>(east));
            var slots = Server.System<ItemSlotsSystem>();
            Assert.That(slots.TryInsert((origin, null), "first", SEntMan.SpawnEntity("CEConnectedTestItem", map.GridCoords), null), Is.True);
            Assert.That(slots.TryInsert((origin, null), "second", SEntMan.SpawnEntity("CEConnectedTestItem", map.GridCoords), null), Is.True);
        });
        await Pair.RunTicksSync(10);
        await Server.WaitAssertion(() =>
        {
            var hands = Server.System<SharedHandsSystem>();
            var connected = Server.System<CEConnectedItemSlotsSystem>();
            var item = SEntMan.SpawnEntity("CEConnectedTestItem", map.GridCoords);
            Assert.That(hands.TryPickup(actor, item, "hand"), Is.True);
            Assert.That(connected.TryInsertFromHand(actor, item, origin, out var destination), Is.True);
            Assert.That(destination, Is.EqualTo(north));
            Server.System<SharedTransformSystem>().Unanchor(north, SEntMan.GetComponent<TransformComponent>(north));
        });
        await Pair.RunTicksSync(10);
        await Server.WaitAssertion(() =>
        {
            var item = SEntMan.SpawnEntity("CEConnectedTestItem", map.GridCoords);
            Assert.That(Server.System<SharedHandsSystem>().TryPickup(actor, item, "hand"), Is.True);
            Assert.That(Server.System<CEConnectedItemSlotsSystem>().TryInsertFromHand(actor, item, origin, out var destination), Is.True);
            Assert.That(destination, Is.EqualTo(east));
        });
        await Pair.RunTicksSync(5);
    }

}
