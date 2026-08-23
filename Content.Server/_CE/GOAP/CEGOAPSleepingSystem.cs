using System.Numerics;
using Content.Shared._CE.GOAP.Components;
using Content.Shared.Damage.Systems;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server._CE.GOAP;

/// <summary>
/// Manages sleeping GOAP entities. Wakes them on:
/// - Player proximity (iterates players, not mobs, for performance)
/// - Damage received
/// - Chain reaction from a nearby mob waking
/// </summary>
public sealed partial class CEGOAPSleepingSystem : EntitySystem
{
    [Dependency] private CEGOAPSystem _goap = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private IGameTiming _timing = default!;

    /// <summary>
    /// How often proximity checks run.
    /// </summary>
    private static readonly TimeSpan ProximityCheckInterval = TimeSpan.FromSeconds(1);

    private TimeSpan _nextProximityCheck;

    private readonly HashSet<Entity<CEGOAPSleepingComponent>> _nearbyBuffer = new();

    public override void Initialize()
    {
        base.Initialize();

        // Wake on damage
        SubscribeLocalEvent<CEGOAPSleepingComponent, DamageDealtEvent>(OnDamageDealt);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_timing.CurTime < _nextProximityCheck)
            return;

        _nextProximityCheck = _timing.CurTime + ProximityCheckInterval;

        // CrystallEdge: Rogue gated this on CEDungeonPlayerComponent (procgen dungeon instances),
        // which doesn't exist in this fork. Any player near a sleeping GOAP mob wakes it.
        var playerQuery = EntityQueryEnumerator<ActorComponent, TransformComponent>();
        while (playerQuery.MoveNext(out _, out _, out var xform))
        {
            if (xform.MapUid is null)
                continue;

            _nearbyBuffer.Clear();
            _lookup.GetEntitiesInRange(xform.Coordinates, 8f, _nearbyBuffer);

            foreach (var sleeping in _nearbyBuffer)
            {
                var mobPos = _transform.GetWorldPosition(sleeping);
                var playerPos = _transform.GetWorldPosition(xform);
                var distance = Vector2.Distance(mobPos, playerPos);

                if (distance <= sleeping.Comp.WakeRadius)
                    WakeMob(sleeping);
            }
        }
    }

    private void OnDamageDealt(Entity<CEGOAPSleepingComponent> ent, ref DamageDealtEvent args)
    {
        WakeMob(ent);
    }

    /// <summary>
    /// Wakes a sleeping mob: removes the sleeping marker, re-evaluates GOAP awake status,
    /// and chain-wakes nearby sleeping mobs.
    /// </summary>
    public void WakeMob(Entity<CEGOAPSleepingComponent> ent)
    {
        if (TerminatingOrDeleted(ent))
            return;

        // Must use RemComp (not Deferred) so HasComp check in OnCheckAwake
        // sees the component as absent when UpdateAwakeStatus runs immediately after.
        RemComp<CEGOAPSleepingComponent>(ent);

        // Re-evaluate GOAP awake status — with the sleeping component removed,
        // the normal wake check in CEGOAPSystem will now succeed.
        if (TryComp<CEGOAPComponent>(ent, out var goap))
            _goap.UpdateAwakeStatus((ent, goap));
    }
}
