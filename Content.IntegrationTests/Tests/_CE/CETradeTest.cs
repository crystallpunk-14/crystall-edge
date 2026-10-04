#nullable enable
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Content.Server._CE.Trade;
using Content.Shared._CE.Trade;
using Content.Shared._CE.Trade.Components;
using Content.Shared._CE.Trade.Prototypes;
using Content.Shared.Hands.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._CE;

[TestFixture]
public sealed class CETradeTest
{
    private static readonly EntProtoId Table = "CETradeTableHorticulture";
    private static readonly EntProtoId Human = "CEMobHuman";
    private static readonly EntProtoId Money = "CECoinPlatinum10";

    [Test]
    public async Task RestockTradeAndClear()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var entMan = server.EntMan;
        var protoMan = server.ResolveDependency<IPrototypeManager>();

        var map = await pair.CreateTestMap();

        await server.WaitAssertion(() =>
        {
            var trade = entMan.System<CETradeSystem>();
            var table = entMan.SpawnEntity(Table, new EntityCoordinates(map.Grid.Owner, new Vector2(0.5f, 0.5f)));
            var tableEnt = (table, entMan.GetComponent<CETradeTableComponent>(table));

            for (var i = 0; i < tableEnt.Item2.Slots.Count; i++)
            {
                Assert.That(trade.TryRestock(tableEnt), $"Restock {i} should fill an empty slot");
            }

            Assert.That(trade.TryRestock(tableEnt), Is.False, "A full table can't be restocked");
            Assert.That(trade.HasFreeSlot(tableEnt), Is.False);

            var offers = Offers(entMan, table);
            Assert.That(offers.Select(o => o.Comp.Slot).Distinct().Count(), Is.EqualTo(offers.Count), "Each offer takes its own slot");

            foreach (var offer in offers)
            {
                var proto = protoMan.Index(offer.Comp.Offer!.Value);
                Assert.That(trade.GetPreview(proto, offer.Comp.ReceivePrice), Is.Not.Null, $"{proto.ID} must have something to show");
            }

            // Selling offers show coins.
            foreach (var sell in protoMan.EnumeratePrototypes<CETradeOfferPrototype>().Where(o => o.Reward.Count == 0 && o.Preview == null))
            {
                Assert.That(trade.GetPreview(sell, 25), Is.EqualTo(new EntProtoId("CECoinSilver1")), $"{sell.ID} should look like coins");
            }

            Assert.That(trade.TryClearOne(tableEnt), "Clearing a stocked table should remove an offer");
        });

        await server.WaitRunTicks(1);

        await server.WaitAssertion(() =>
        {
            var trade = entMan.System<CETradeSystem>();
            var table = entMan.EntityQuery<CETradeTableComponent>().Select(t => t.Owner).Single();
            Assert.That(Offers(entMan, table), Has.Count.EqualTo(3));

            // Fill the freed slot until it holds a pure money-for-goods offer to buy.
            var buyer = entMan.SpawnEntity(Human, new EntityCoordinates(map.Grid.Owner, new Vector2(1.5f, 0.5f)));
            Entity<CETradeOfferComponent>? buy = null;
            for (var attempt = 0; attempt < 200 && buy == null; attempt++)
            {
                buy = Offers(entMan, table).FirstOrDefault(o =>
                    o.Comp.PayPrice > 0 && protoMan.Index(o.Comp.Offer!.Value).Cost.Count == 0);

                if (buy != null)
                    break;

                entMan.DeleteEntity(Offers(entMan, table)[0]);
                trade.TryRestock((table, entMan.GetComponent<CETradeTableComponent>(table)));
            }

            Assert.That(buy, Is.Not.Null, "Horticulture should roll a coins-for-goods offer");

            Assert.That(trade.TryTrade(buy!.Value, buyer), Is.False, "Can't buy without money");

            var coins = entMan.SpawnEntity(Money, entMan.GetComponent<TransformComponent>(buyer).Coordinates);
            entMan.System<SharedHandsSystem>().TryPickupAnyHand(buyer, coins);

            Assert.That(trade.TryTrade(buy.Value, buyer), "Should buy with enough money");
        });

        await pair.CleanReturnAsync();
    }

    private static List<Entity<CETradeOfferComponent>> Offers(IEntityManager entMan, EntityUid table)
    {
        var result = new List<Entity<CETradeOfferComponent>>();
        var children = entMan.GetComponent<TransformComponent>(table).ChildEnumerator;
        while (children.MoveNext(out var child))
        {
            if (entMan.TryGetComponent<CETradeOfferComponent>(child, out var offer) && !entMan.IsQueuedForDeletion(child))
                result.Add((child, offer));
        }

        return result;
    }
}
