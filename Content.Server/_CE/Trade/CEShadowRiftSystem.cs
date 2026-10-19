using Content.Server._CE.Trade.Components;
using Content.Shared._CE.DayCycle;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._CE.Trade;

public sealed partial class CEShadowRiftSystem : EntitySystem
{
    [Dependency] private CEDayCycleSystem _dayCycle = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IRobustRandom _random = default!;

    [SubscribeLocalEvent]
    private void OnMapInit(Entity<CEShadowRiftComponent> ent, ref MapInitEvent args)
    {
        if (_transform.GetMap(ent.Owner) is { } map && _dayCycle.IsDayNow(map))
            StartShift(ent);
    }

    [SubscribeLocalEvent]
    private void OnStartDay(CEStartDayEvent args)
    {
        var query = EntityQueryEnumerator<CEShadowRiftComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var rift, out var xform))
        {
            if (xform.MapUid == args.MapUid)
                StartShift((uid, rift));
        }
    }

    [SubscribeLocalEvent]
    private void OnShutdown(Entity<CEShadowRiftComponent> ent, ref ComponentShutdown args)
    {
        DissolveTraders(ent);
    }

    /// <summary>
    /// Dissolves the traders released before and queues a full new batch.
    /// </summary>
    private void StartShift(Entity<CEShadowRiftComponent> ent)
    {
        DissolveTraders(ent);
        ent.Comp.PendingSpawns = ent.Comp.Count;
        ent.Comp.NextSpawnAt = _timing.CurTime;
    }

    private void DissolveTraders(Entity<CEShadowRiftComponent> ent)
    {
        foreach (var trader in ent.Comp.Traders)
        {
            if (TerminatingOrDeleted(trader))
                continue;

            if (Transform(trader).MapUid is { } map && !TerminatingOrDeleted(map))
                SpawnAtPosition(ent.Comp.DissolveEffect, Transform(trader).Coordinates);

            QueueDel(trader);
        }

        ent.Comp.Traders.Clear();
    }

    public override void Update(float frameTime)
    {
        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<CEShadowRiftComponent>();
        while (query.MoveNext(out var uid, out var rift))
        {
            if (rift.PendingSpawns <= 0 || now < rift.NextSpawnAt || rift.Spawns.Count == 0)
                continue;

            rift.PendingSpawns--;
            rift.NextSpawnAt = now + rift.Interval;
            rift.Traders.Add(SpawnAtPosition(_random.Pick(rift.Spawns), Transform(uid).Coordinates));
        }
    }
}
