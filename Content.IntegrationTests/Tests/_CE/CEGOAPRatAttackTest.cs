#nullable enable
using System.Numerics;
using Content.Server._CE.GOAP;
using Content.Server._CE.GOAP.Sensors;
using Content.Shared._CE.GOAP.Components;
using Content.Shared._CE.GOAP.Prototypes;
using Content.Shared.Damage.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.IntegrationTests.Tests._CE;

/// <summary>
/// End-to-end GOAP check: a rat next to a hostile human must notice it, plan an attack and bite it.
/// Each step of the chain is asserted separately so a failure points at the broken link.
/// </summary>
[TestFixture]
public sealed class CEGOAPRatAttackTest
{
    private static readonly EntProtoId Rat = "CEMobRat";
    private static readonly EntProtoId Human = "CEMobHuman";
    private static readonly ProtoId<CEGOAPTargetPrototype> EnemySlot = "Enemy";
    private static readonly ProtoId<CEGOAPConditionPrototype> EnemyVisible = "EnemyVisible";

    [Test]
    public async Task RatAttacksNearbyHuman()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var entMan = server.EntMan;

        var map = await pair.CreateTestMap();

        EntityUid human = default;
        EntityUid rat = default;

        await server.WaitPost(() =>
        {
            var mapSystem = entMan.System<SharedMapSystem>();
            var tileMan = server.ResolveDependency<ITileDefinitionManager>();
            var tile = new Tile(tileMan["Plating"].TileId);

            for (var x = 0; x < 5; x++)
            {
                for (var y = 0; y < 5; y++)
                {
                    mapSystem.SetTile(map.Grid.Owner, map.Grid.Comp, new Vector2i(x, y), tile);
                }
            }

            human = entMan.SpawnEntity(Human, new EntityCoordinates(map.Grid.Owner, new Vector2(2.5f, 2.5f)));
            rat = entMan.SpawnEntity(Rat, new EntityCoordinates(map.Grid.Owner, new Vector2(3.5f, 2.5f)));
        });

        var timing = server.ResolveDependency<IGameTiming>();
        await pair.RunTicksSync(timing.TickRate * 6);

        await server.WaitAssertion(() =>
        {
            var goap = entMan.GetComponent<CEGOAPComponent>(rat);
            var damageable = entMan.System<DamageableSystem>();

            Assert.Multiple(() =>
            {
                Assert.That(entMan.HasComponent<CEActiveGOAPComponent>(rat), "Rat GOAP should be awake");

                Assert.That(goap.Targets.ContainsKey(EnemySlot),
                    $"Rat should have the {EnemySlot} target slot from its behaviors");

                Assert.That(entMan.TryGetComponent<CEGOAPHasTargetSensorComponent>(rat, out var sensor)
                            && sensor.Entries.Count > 0,
                    "Rat should have the HasTarget sensor attached by its behaviors");

                Assert.That(goap.Knowledge.ContainsKey(human), "Rat should have perceived the human");

                Assert.That(entMan.System<CEGOAPSystem>().ResolveTarget(rat, EnemySlot).Entity, Is.EqualTo(human),
                    $"The {EnemySlot} slot should resolve to the human");

                Assert.That(goap.WorldState.TryGetValue(EnemyVisible, out var visible) && visible,
                    $"{EnemyVisible} should be true while the human is in sight");

                Assert.That(damageable.GetTotalDamage(human).Float(), Is.GreaterThan(0f),
                    "Human should have been bitten by the rat");
            });
        });

        await pair.CleanReturnAsync();
    }
}
