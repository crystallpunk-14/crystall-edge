#nullable enable
using System.Numerics;
using Content.Server._CE.GOAP;
using Content.Server._CE.GOAP.Actions;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Gravity;
using Content.Shared._CE.GOAP.Components;
using Content.Shared._CE.GOAP.Prototypes;
using Content.Shared.Atmos;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.Gravity;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.IntegrationTests.Tests._CE;

/// <summary>
/// End-to-end GOAP check: a rat a few tiles from a hostile human, behind a fence, must notice the human,
/// plan a melee attack on it, get over or through the fence on the way and bite it.
/// Each step of the chain is asserted separately so a failure points at the broken link.
/// </summary>
[TestFixture]
public sealed class CEGOAPRatAttackTest
{
    private static readonly EntProtoId Rat = "CEMobRat";
    private static readonly EntProtoId Human = "CEMobHuman";
    private static readonly EntProtoId Fence = "CEFenceWooden";
    private static readonly ProtoId<CEGOAPTargetPrototype> EnemySlot = "Enemy";
    private static readonly ProtoId<DamageTypePrototype> Bite = "Piercing";

    [Test]
    public async Task RatAttacksNearbyHuman()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var entMan = server.EntMan;
        var timing = server.ResolveDependency<IGameTiming>();

        var map = await pair.CreateTestMap();

        EntityUid human = default;
        EntityUid rat = default;

        await server.WaitPost(() =>
        {
            // Gravity, so mobs walk instead of drifting off the platform after a lunge.
            var gravity = entMan.EnsureComponent<GravityComponent>(map.Grid);
            entMan.System<GravitySystem>().EnableGravity(map.Grid, gravity);

            // Breathable air, so the human only takes damage from the rat.
            var moles = new float[Atmospherics.AdjustedNumberOfGases];
            moles[(int) Gas.Oxygen] = 21.824779f;
            moles[(int) Gas.Nitrogen] = 82.10312f;
            entMan.System<AtmosphereSystem>().SetMapAtmosphere(map.MapUid, false, new GasMixture(moles, Atmospherics.T20C));

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

            // A fence wall across the whole platform, so the rat can't walk around it. Fence colliders are a strip
            // along one tile edge (north by default); turned 90 degrees they block the west-east way instead.
            var xformSystem = entMan.System<SharedTransformSystem>();
            for (var y = 0; y < 5; y++)
            {
                var fence = entMan.SpawnEntity(Fence, new EntityCoordinates(map.Grid.Owner, new Vector2(2.5f, y + 0.5f)));
                xformSystem.SetLocalRotation(fence, Angle.FromDegrees(90));
            }

            human = entMan.SpawnEntity(Human, new EntityCoordinates(map.Grid.Owner, new Vector2(4.5f, 2.5f)));
            rat = entMan.SpawnEntity(Rat, new EntityCoordinates(map.Grid.Owner, new Vector2(0.5f, 2.5f)));
        });

        // Perception: the rat should notice the human through the fence within a couple of eye scans.
        await pair.RunTicksSync(timing.TickRate * 4);

        await server.WaitAssertion(() =>
        {
            var goap = entMan.GetComponent<CEGOAPComponent>(rat);

            Assert.Multiple(() =>
            {
                Assert.That(entMan.HasComponent<CEActiveGOAPComponent>(rat), "Rat GOAP should be awake");

                Assert.That(goap.Targets.ContainsKey(EnemySlot),
                    $"Rat should have the {EnemySlot} target slot from its behaviors");

                Assert.That(goap.Knowledge.ContainsKey(human), "Rat should have perceived the human");

                Assert.That(entMan.System<CEGOAPSystem>().ResolveTarget(rat, EnemySlot).Entity, Is.EqualTo(human),
                    $"The {EnemySlot} slot should resolve to the human");

                // Walking up to the human is part of the attack, not a separate step of the plan.
                Assert.That(goap.CurrentActionIndex < goap.CurrentPlan.Count
                            && goap.CurrentPlan[goap.CurrentActionIndex] is CEGOAPMeleeAttackAction { Target: var slot }
                            && slot == EnemySlot,
                    $"Rat should be carrying out a melee attack on its {EnemySlot} slot");
            });
        });

        // Steering: the rat has to get over or through the fence before it can bite.
        await pair.RunTicksSync(timing.TickRate * 20);

        await server.WaitAssertion(() =>
        {
            var damage = entMan.System<DamageableSystem>().GetAllDamage(human);
            Assert.That(damage.DamageDict.TryGetValue(Bite, out var bitten) ? bitten.Float() : 0f, Is.GreaterThan(0f),
                "Human should have been bitten by the rat");
        });

        await pair.CleanReturnAsync();
    }
}
