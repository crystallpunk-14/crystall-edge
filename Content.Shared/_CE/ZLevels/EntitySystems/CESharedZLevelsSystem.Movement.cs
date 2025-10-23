using Content.Shared.ActionBlocker;
using Content.Shared.Chasm;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Ghost;
using Content.Shared.Stunnable;
using Content.Shared.Throwing;
using JetBrains.Annotations;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared._CE.ZLevels.EntitySystems;

public abstract partial class CESharedZLevelsSystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] protected readonly IPrototypeManager Proto = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly ActionBlockerSystem _blocker = default!;

    public const int MaxZLevelsBelowRendering = 3;
    public const int MaxZLevelsAboveRendering = 1;

    private const float ZGravityForce = 7.0f;

    /// <summary>
    /// The minimum speed required to trigger LandEvent events.
    /// </summary>
    private const float ImpactVelocityLimit = 2.0f;

    private EntityQuery<MapGridComponent> _gridQuery;
    private EntityQuery<GhostComponent> _ghostQuery;

    private void InitMovement()
    {
        _gridQuery = GetEntityQuery<MapGridComponent>();
        _ghostQuery = GetEntityQuery<GhostComponent>();

        SubscribeLocalEvent<DamageableComponent, CEZLevelHitEvent>(OnFallEvent);
        SubscribeLocalEvent<PhysicsComponent, MapInitEvent>(OnPhysicMapInit);
    }

    private void OnFallEvent(Entity<DamageableComponent> ent, ref CEZLevelHitEvent args)
    {
        var knockdownTime = MathF.Min(args.Velocity * 0.5f, 10f);
        _stun.TryKnockdown(ent.Owner, TimeSpan.FromSeconds(knockdownTime));

        var damageType = Proto.Index<DamageTypePrototype>("Blunt");
        var damageAmount = Math.Pow(args.Velocity, 2.25f);

        _damage.TryChangeDamage(ent.Owner, new DamageSpecifier(damageType, damageAmount));
    }

    private void OnPhysicMapInit(Entity<PhysicsComponent> ent, ref MapInitEvent args)
    {
        if (_ghostQuery.HasComp(ent))
            return;

        EnsureComp<CEZLevelPhysicsComponent>(ent);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<CEZLevelPhysicsComponent, TransformComponent, PhysicsComponent>();
        while (query.MoveNext(out var uid, out var zPhys, out var xform, out var physics))
        {
            //Gravity force application
            ApplyZGravityForce(uid, zPhys, xform, physics, frameTime);

            //Movement application
            zPhys.LocalHeight += zPhys.Velocity * frameTime;
            if (zPhys.LocalHeight < 0) //Falling down
            {
                if (HasGround(uid))
                {
                    zPhys.LocalHeight = 0;

                    if (MathF.Abs(zPhys.Velocity) >= ImpactVelocityLimit)
                    {
                        RaiseLocalEvent(uid, new CEZLevelHitEvent(MathF.Abs(zPhys.Velocity)));
                        var land = new LandEvent(null, true);
                        RaiseLocalEvent(uid, ref land);
                    }

                    zPhys.Velocity = 0;
                }
                else //Fall down
                {
                    if (TryMoveDownOrChasm(uid))
                        zPhys.LocalHeight += 1;
                }
            }
            else if (zPhys.LocalHeight > 1) //Going up
            {
                if (HasRoof(uid)) //Hit roof
                {
                    zPhys.LocalHeight = 1;

                    if (MathF.Abs(zPhys.Velocity) >= ImpactVelocityLimit)
                    {
                        RaiseLocalEvent(uid, new CEZLevelHitEvent(zPhys.Velocity));
                        var land = new LandEvent(null, true);
                        RaiseLocalEvent(uid, ref land);
                    }

                    zPhys.Velocity = 0;
                }
                else //Move up
                {
                    if (TryMoveUp(uid))
                        zPhys.LocalHeight -= 1;
                }
            }
        }
    }

    private void ApplyZGravityForce(EntityUid uid, CEZLevelPhysicsComponent zPhys, TransformComponent xform, PhysicsComponent physics, float frameTime)
    {
        if (physics.BodyStatus == BodyStatus.InAir)
            return;
        if (_ghostQuery.HasComp(uid))
            return;
        if (xform.ParentUid != xform.MapUid)
            return;

        var newVelocity = zPhys.Velocity - ZGravityForce * frameTime;
        SetZVelocity((uid, zPhys), newVelocity);
    }

    private void SetZVelocity(Entity<CEZLevelPhysicsComponent> zPhys, float velocity)
    {
        zPhys.Comp.Velocity = velocity;
        Dirty(zPhys);
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

    public bool HasRoof(EntityUid target)
    {
        var mapUid = Transform(target).MapUid;

        if (mapUid is null)
            return false;

        if (!TryMapUp(mapUid.Value, out var mapAbove, out var mapAboveUid))
            return false;

        if (!_gridQuery.TryComp(mapAboveUid.Value, out var mapAboveGrid))
            return false;

        if (_map.TryGetTileRef(mapAboveUid.Value, mapAboveGrid, _transform.GetWorldPosition(target), out var tileRef) && !tileRef.Tile.IsEmpty)
            return true;

        return false;
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

    [PublicAPI]
    public bool TryMoveDownOrChasm(EntityUid ent)
    {
        if (TryMoveDown(ent))
            return true;

        //welp, that default Chasm behavior. Not really good, but ok for now.
        if (HasComp<ChasmFallingComponent>(ent))
            return false; //Already falling

        var audio = new SoundPathSpecifier("/Audio/Effects/falling.ogg");
        _audio.PlayPredicted(audio, Transform(ent).Coordinates, ent);
        var falling = AddComp<ChasmFallingComponent>(ent);
        falling.NextDeletionTime = _timing.CurTime + falling.DeletionTime;
        _blocker.UpdateCanMove(ent);

        return false;
    }
}

public sealed class CEZLevelHitEvent(float velocity) : EntityEventArgs
{
    public float Velocity = velocity;
}
