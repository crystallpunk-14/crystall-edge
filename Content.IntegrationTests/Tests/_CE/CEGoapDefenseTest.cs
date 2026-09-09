using System.Numerics;
using Content.IntegrationTests.Fixtures;
using Content.Server._CE.GOAP.Actions;
using Content.Server._CE.GOAP.Classifiers;
using Content.Server._CE.GOAP.Combat;
using Content.Server._CE.NPC;
using Content.Shared._CE.EntityEffect;
using Content.Shared._CE.EntityEffect.Effects;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Movement.Components;
using Content.Shared.NPC.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using CEDamage = Content.Shared._CE.EntityEffect.Effects.Damage;

namespace Content.IntegrationTests.Tests._CE;

[TestFixture]
public sealed class CEGoapDefenseTest : GameTest
{
    public override PoolSettings PoolSettings => new() { Connected = false };

    [TestPrototypes]
    private const string Prototypes = """
- type: npcFaction
  id: CEDefenseTestFaction
  friendly: [CEDefenseTestFaction]

- type: entity
  id: CEDefenseTestTarget
  components:
  - type: NpcFactionMember
    factions: [CEDefenseTestFaction]
  - type: Damageable
    damageContainer: Biological
  # Native DamageDealtEvent stores damage only on Injurable entities.
  - type: Injurable
  - type: Physics
    bodyType: Static
  - type: Fixtures
    fixtures:
      body:
        shape: !type:PhysShapeCircle
          radius: 0.2
        layer: [MobLayer]
        mask: [MobMask]

- type: entity
  id: CEDefenseTestWatcher
  parent: CEDefenseTestTarget
  components:
  - type: CEGOAP
  - type: CEGOAPEyesPerceptor
    updateInterval: 0.1
    crossZLevelVision: false
  - type: CEGOAPKnowledgeCache
  - type: CEGOAPPainPerceptor
    retaliationDuration: 5
  - type: CEGOAPAllyThreatPerceptor
    range: 5
  - type: CENPCMovement
  - type: CombatMode
""";

    [Test]
    public async Task StationaryVisibleAllyChangesClassificationWithoutMoving()
    {
        var map = await Pair.CreateTestMap();
        EntityUid watcher = default;
        EntityUid target = default;
        MapCoordinates initialTargetPosition = default;
        await Server.WaitPost(() =>
        {
            watcher = SpawnWatcher(map.GridCoords);
            target = SpawnTarget(map.GridCoords.Offset(Vector2.UnitX));
            initialTargetPosition = Server.System<SharedTransformSystem>().GetMapCoordinates(target);
        });

        await Server.WaitRunTicks(15);
        await Server.WaitAssertion(() =>
        {
            Assert.That(Knowledge(watcher).Allies, Does.Contain(target));
            Server.System<NpcFactionSystem>().AggroEntity(watcher, target);
        });
        await Server.WaitRunTicks(15);
        await Server.WaitAssertion(() =>
        {
            Assert.That(Knowledge(watcher).Enemies, Does.Contain(target));
            Assert.That(Knowledge(watcher).Allies, Does.Not.Contain(target));
            // Grid traversal may change the parent without moving the entity in the world.
            Assert.That(Server.System<SharedTransformSystem>().GetMapCoordinates(target),
                Is.EqualTo(initialTargetPosition));
            Server.System<NpcFactionSystem>().DeAggroEntity(watcher, target);
        });
        await Server.WaitRunTicks(15);
        await Server.WaitAssertion(() =>
        {
            Assert.That(Knowledge(watcher).Allies, Does.Contain(target));
            Assert.That(Knowledge(watcher).Enemies, Does.Not.Contain(target));
        });
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task NativeArcHitsKnownThreatAndFiltersAllyOutsideCircleAction(bool defensive)
    {
        var map = await Pair.CreateTestMap();
        EntityUid guard = default;
        EntityUid aggressor = default;
        EntityUid ally = default;
        await Server.WaitAssertion(() =>
        {
            PrepareArena(map.Grid, map.Tile.Tile);
            guard = SpawnWatcher(map.GridCoords);
            aggressor = SpawnTarget(map.GridCoords.Offset(new Vector2(0.9f, -0.35f)));
            ally = SpawnTarget(map.GridCoords.Offset(new Vector2(0.9f, 0.35f)));
            if (defensive)
                SEntMan.AddComponent<CEGOAPDefensiveCombatComponent>(guard);

            // Real damage, rather than writing the knowledge cache, establishes the threat.
            Assert.That(Server.System<DamageableSystem>().TryChangeDamage(guard, BluntDamage(), origin: aggressor), Is.True);
            Assert.That(Knowledge(guard).Enemies, Does.Contain(aggressor));
            Assert.That(SEntMan.HasComponent<CEGOAPCircleMeleeComponent>(guard), Is.False);
        });
        await Server.WaitRunTicks(1);
        await Server.WaitAssertion(() =>
        {
            Swing(guard);
            Assert.That(Damage(aggressor), Is.EqualTo(2));
            Assert.That(Damage(ally), Is.EqualTo(defensive ? 0 : 2));
        });
    }

    [Test]
    public async Task LateWitnessRecognizesDefenderAndTargetsOriginalAggressor()
    {
        var map = await Pair.CreateTestMap();
        EntityUid guard = default;
        EntityUid aggressor = default;
        EntityUid protectedAlly = default;
        EntityUid lateWitness = default;
        await Server.WaitAssertion(() =>
        {
            PrepareArena(map.Grid, map.Tile.Tile);
            guard = SpawnWatcher(map.GridCoords);
            SEntMan.AddComponent<CEGOAPDefensiveCombatComponent>(guard);
            protectedAlly = SpawnTarget(map.GridCoords.Offset(-Vector2.UnitX));
            aggressor = SpawnTarget(map.GridCoords.Offset(new Vector2(0.9f, -0.35f)));
            Assert.That(Server.System<DamageableSystem>().TryChangeDamage(protectedAlly, BluntDamage(), origin: aggressor), Is.True);
            Assert.That(Knowledge(guard).Enemies, Does.Contain(aggressor));

            // The witness did not exist during the first hit and cannot remember its origin.
            lateWitness = SpawnWatcher(map.GridCoords.Offset(new Vector2(0.9f, 0.35f)));
            Assert.That(Knowledge(lateWitness).Enemies, Is.Empty);
        });
        await Server.WaitRunTicks(1);
        await Server.WaitAssertion(() =>
        {
            Swing(guard);
            Assert.That(Damage(aggressor), Is.EqualTo(2));
            Assert.That(Damage(lateWitness), Is.Zero);
            Assert.That(Damage(protectedAlly), Is.EqualTo(2));
            Assert.That(Knowledge(lateWitness).Enemies, Does.Contain(aggressor));
            Assert.That(Knowledge(lateWitness).Enemies, Does.Not.Contain(guard));
            Assert.That(Knowledge(guard).Enemies, Does.Not.Contain(lateWitness));
        });
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task UrgentActionsRestoreCalmWalkingAndPreserveExistingRotationLock(bool alreadyLocked)
    {
        var map = await Pair.CreateTestMap();
        await Server.WaitAssertion(() =>
        {
            var actor = SpawnWatcher(map.GridCoords);
            var movement = SEntMan.GetComponent<CENPCMovementComponent>(actor);
            Assert.That(movement.Walking, Is.True);
            var flee = new CEGOAPFleeAction();
            flee.RaiseStartup(actor, SEntMan);
            Assert.That(movement.Walking, Is.False);
            flee.RaiseShutdown(actor, SEntMan);
            Assert.That(movement.Walking, Is.True);

            if (alreadyLocked)
                SEntMan.AddComponent<NoRotateOnMoveComponent>(actor);
            var circle = new CEGOAPCircleMeleeAction();
            circle.RaiseStartup(actor, SEntMan);
            Assert.That(movement.Walking, Is.False);
            Assert.That(SEntMan.HasComponent<NoRotateOnMoveComponent>(actor), Is.True);
            circle.RaiseShutdown(actor, SEntMan);
            Assert.That(movement.Walking, Is.True);
            Assert.That(SEntMan.HasComponent<NoRotateOnMoveComponent>(actor), Is.EqualTo(alreadyLocked));
        });
    }

    private EntityUid SpawnWatcher(EntityCoordinates coords)
        => SEntMan.SpawnEntity("CEDefenseTestWatcher", coords);

    private EntityUid SpawnTarget(EntityCoordinates coords)
        => SEntMan.SpawnEntity("CEDefenseTestTarget", coords);

    private CEGOAPKnowledgeCacheComponent Knowledge(EntityUid actor)
        => SEntMan.GetComponent<CEGOAPKnowledgeCacheComponent>(actor);

    private float Damage(EntityUid actor)
        => Server.System<DamageableSystem>().GetTotalDamage(actor).Float();

    private static DamageSpecifier BluntDamage()
    {
        var damage = new DamageSpecifier();
        damage.DamageDict["Blunt"] = 2;
        return damage;
    }

    private void PrepareArena(Entity<MapGridComponent> grid, Tile tile)
    {
        // CreateTestMap supplies one tile; the arc scene spans both sides of its origin.
        var maps = Server.System<SharedMapSystem>();
        for (var x = -2; x <= 2; x++)
        for (var y = -2; y <= 2; y++)
            maps.SetTile(grid, new Vector2i(x, y), tile);
    }

    private void Swing(EntityUid source)
    {
        // Exercise the real raycast, target-filter event and native damage pipeline.
        // No Circle action is running when this animation-equivalent keyframe lands.
        var arc = new WeaponArcAttack
        {
            ArcWidth = 90,
            Effects = [new CEDamage { DamageSpec = BluntDamage(), ColorFlash = false }],
        };
        arc.Effect(new CEEntityEffectArgs(SEntMan, source, null, Angle.FromWorldVec(Vector2.UnitX), 1, null, null));
    }
}
