using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Ghost;
using Content.Shared.Stunnable;
using Content.Shared.Throwing;
using JetBrains.Annotations;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Network;
using Robust.Shared.Physics.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared._CE.ZLevels.EntitySystems;

public abstract partial class CESharedZLevelsSystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    private readonly TimeSpan _physicsUpdateDelay = TimeSpan.FromSeconds(0.1f);
    private TimeSpan _nextPhysicsUpdate = TimeSpan.Zero;

    private EntityQuery<MapGridComponent> _gridQuery;
    private EntityQuery<GhostComponent> _ghostQuery;

    private void InitMovement()
    {
        _gridQuery = GetEntityQuery<MapGridComponent>();
        _ghostQuery = GetEntityQuery<GhostComponent>();

        SubscribeLocalEvent<DamageableComponent, CEZLevelFallEvent>(OnFallEvent);
    }

    private void OnFallEvent(Entity<DamageableComponent> ent, ref CEZLevelFallEvent args)
    {
        _stun.TryKnockdown(ent.Owner, TimeSpan.FromSeconds(args.FallingDistance * 0.5));
        var damageType = _proto.Index<DamageTypePrototype>("Blunt");
        var damageAmount = 20 * Math.Pow(1.5, args.FallingDistance - 1);
        _damage.TryChangeDamage(ent.Owner, new DamageSpecifier(damageType, damageAmount));
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_net.IsClient)
            return;

        if (_timing.CurTime < _nextPhysicsUpdate)
            return;

        _nextPhysicsUpdate = _timing.CurTime + _physicsUpdateDelay;

        var query = EntityQueryEnumerator<TransformComponent, PhysicsComponent>();
        while (query.MoveNext(out var uid, out var xform, out var physics))
        {
            if (physics.BodyStatus == BodyStatus.InAir)
                continue;
            if (_ghostQuery.HasComp(uid))
                continue;
            if (xform.ParentUid != xform.MapUid)
                continue;
            if (HasGround(uid))
                continue;

            var ev = new CEBeforeZLevelFallingEvent();
            RaiseLocalEvent(uid, ev);

            if (ev.Cancelled)
                continue;

            Fallout(uid);
        }

        //Process falled entities
        var falledQuery = EntityQueryEnumerator<CEFallingZComponent, PhysicsComponent>();
        while (falledQuery.MoveNext(out var uid, out var falling, out var physics))
        {
            if (physics.BodyStatus == BodyStatus.InAir) //Wow, we start flying mid-falling!
            {
                RemCompDeferred<CEFallingZComponent>(uid);
                continue;
            }
            if (HasGround(uid))
            {
                var ev = new CEZLevelFallEvent(falling.FallingDistance);
                RaiseLocalEvent(uid, ev);

                var landEv = new LandEvent(null, true);
                RaiseLocalEvent(uid, ref landEv);

                RemCompDeferred<CEFallingZComponent>(uid);
                continue;
            }
        }
    }

    public bool HasGround(EntityUid target)
    {
        var map = Transform(target).MapUid;
        if (!_gridQuery.TryComp(map, out var mapGrid))
            return true; //uhhh, ehhh, ok?

        if (_map.TryGetTileRef(map.Value, mapGrid, _transform.GetWorldPosition(target), out var tileRef) && !tileRef.Tile.IsEmpty)
            return true;

        return false;
    }

    /// <summary>
    /// We try to move the target down. If there is nowhere else to move it down, we hit the ground and break our legs.
    /// </summary>
    private void Fallout(EntityUid target)
    {
        var falling = EnsureComp<CEFallingZComponent>(target);

        if (!HasGround(target) && TryMoveDown(target))
        {
            falling.FallingDistance++;
        }
    }

    [PublicAPI]
    public bool TryMove(EntityUid ent, int offset)
    {
        var xform = Transform(ent);
        var map = xform.MapUid;

        if (map is null)
            return false;

        if (!TryMapOffset(map.Value, offset, out var targetMap, out _))
            return false;

        _transform.SetMapCoordinates(ent, new MapCoordinates(_transform.GetWorldPosition(ent), targetMap.Value));
        return true;
    }

    [PublicAPI]
    public bool TryMoveUp(EntityUid ent)
    {
        return TryMove(ent, 1);
    }

    [PublicAPI]
    public bool TryMoveDown(EntityUid ent)
    {
        return TryMove(ent, -1);
    }
}

/// <summary>
/// other systems can prevent falls for various reasons
/// </summary>
public sealed class CEBeforeZLevelFallingEvent : CancellableEntityEventArgs;


public sealed class CEZLevelFallEvent(int fallingDistance) : EntityEventArgs
{
    public int FallingDistance = fallingDistance;
}
