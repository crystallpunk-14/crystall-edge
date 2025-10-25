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
using Robust.Shared.Physics;
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
    [Dependency] private readonly EntityLookupSystem _lookup = default!;

    public const int MaxZLevelsBelowRendering = 3;

    private const float ZGravityForce = 7.0f;
    private const float ZVelocityLimit = 20.0f;

    /// <summary>
    /// The minimum speed required to trigger LandEvent events.
    /// </summary>
    private const float ImpactVelocityLimit = 2.0f;

    private EntityQuery<MapGridComponent> _gridQuery;
    private EntityQuery<CEZLevelSupportComponent> _supportQuery;
    private EntityQuery<GhostComponent> _ghostQuery;

    private void InitMovement()
    {
        _gridQuery = GetEntityQuery<MapGridComponent>();
        _supportQuery = GetEntityQuery<CEZLevelSupportComponent>();
        _ghostQuery = GetEntityQuery<GhostComponent>();

        SubscribeLocalEvent<DamageableComponent, CEZLevelHitEvent>(OnFallDamage);
        SubscribeLocalEvent<PhysicsComponent, CEZLevelHitEvent>(OnFallAreaImpact);
    }

    private void OnFallDamage(Entity<DamageableComponent> ent, ref CEZLevelHitEvent args)
    {
        var knockdownTime = MathF.Min(args.ImpactPower * 0.5f, 10f);
        _stun.TryKnockdown(ent.Owner, TimeSpan.FromSeconds(knockdownTime));

        var damageType = Proto.Index<DamageTypePrototype>("Blunt");
        var damageAmount = args.ImpactPower * args.ImpactPower * MathF.Sqrt(args.ImpactPower);

        _damage.TryChangeDamage(ent.Owner, new DamageSpecifier(damageType, damageAmount));
    }

    /// <summary>
    /// Cause AoE damage in impact point
    /// </summary>
    private void OnFallAreaImpact(Entity<PhysicsComponent> ent, ref CEZLevelHitEvent args)
    {
        var entitiesAround = _lookup.GetEntitiesInRange(ent, 0.25f, LookupFlags.Uncontained);

        foreach (var victim in entitiesAround)
        {
            if (victim == ent.Owner)
                continue;

            var knockdownTime = MathF.Min(args.ImpactPower * ent.Comp.Mass * 0.1f, 10f);
            _stun.TryKnockdown(victim, TimeSpan.FromSeconds(knockdownTime));

            var damageType = Proto.Index<DamageTypePrototype>("Blunt");
            var damageAmount = args.ImpactPower * ent.Comp.Mass * 0.25f;

            _damage.TryChangeDamage(victim, new DamageSpecifier(damageType, damageAmount));
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<CEZPhysicsComponent, TransformComponent, PhysicsComponent>();
        while (query.MoveNext(out var uid, out var zPhys, out var xform, out var physics))
        {
            if (physics.BodyType == BodyType.Static)
                continue;


            var oldVelocity = zPhys.Velocity;
            var oldHeight = zPhys.LocalPosition;

            //Gravity force application
            ApplyZGravityForce(uid, zPhys, xform, physics, frameTime);

            //Movement application
            zPhys.LocalPosition += zPhys.Velocity * frameTime;

            var distanceToGround = DistanceToGround((uid, zPhys));

            if (zPhys.LocalPosition < 0) //We wanna fall down on ZLevel below
            {
                if (distanceToGround <= 0.05f) //But there is ground
                {
                    if (MathF.Abs(zPhys.Velocity) >= ImpactVelocityLimit)
                    {
                        RaiseLocalEvent(uid, new CEZLevelHitEvent(-zPhys.Velocity));
                        var land = new LandEvent(null, true);
                        RaiseLocalEvent(uid, ref land);
                    }

                    //zPhys.LocalPosition = -distanceToGround; //If for some reason the distance to the ground is negative (the player is stuck inside a wall), it should automatically lift him up.
                    zPhys.LocalPosition = 0;
                    zPhys.Velocity = -zPhys.Velocity * zPhys.Bounciness;
                }
                else //Fall down
                {
                    if (TryMoveDownOrChasm(uid))
                        zPhys.LocalPosition += 1;
                }
            }
            else if (zPhys.LocalPosition > 1) //Going up
            {
                if (HasRoof(uid)) //Hit roof
                {
                    if (MathF.Abs(zPhys.Velocity) >= ImpactVelocityLimit)
                    {
                        RaiseLocalEvent(uid, new CEZLevelHitEvent(zPhys.Velocity));
                        var land = new LandEvent(null, true);
                        RaiseLocalEvent(uid, ref land);
                    }

                    zPhys.LocalPosition = 1;
                    zPhys.Velocity = -zPhys.Velocity * zPhys.Bounciness;
                }
                else //Move up
                {
                    if (TryMoveUp(uid))
                        zPhys.LocalPosition -= 1;
                }
            }

            if (Math.Abs(zPhys.Velocity) > ZVelocityLimit)
                zPhys.Velocity = MathF.Sign(zPhys.Velocity) * ZVelocityLimit;

            if (Math.Abs(oldVelocity - zPhys.Velocity) > 0.01f)
                DirtyField(uid, zPhys, nameof(CEZPhysicsComponent.Velocity));

            if (Math.Abs(oldHeight - zPhys.LocalPosition) > 0.01f)
                DirtyField(uid, zPhys, nameof(CEZPhysicsComponent.LocalPosition));
        }
    }

    private void ApplyZGravityForce(EntityUid uid,
        CEZPhysicsComponent zPhys,
        TransformComponent xform,
        PhysicsComponent physics,
        float frameTime)
    {
        if (physics.BodyStatus == BodyStatus.InAir)
            return;
        if (_ghostQuery.HasComp(uid))
            return;
        if (xform.ParentUid != xform.MapUid)
            return;

        if (zPhys.Velocity > 0)
            zPhys.Velocity -=
                ZGravityForce * frameTime * 0.5f; //Gamedesign hack: we have less gravity, when moveing up.
        else
            zPhys.Velocity -= ZGravityForce * frameTime;
    }

    /// <summary>
    /// Returns the distance to the floor. Returns <see cref="maxChecks"/> if the distance is too great.
    /// </summary>
    /// <param name="target">The entity, the distance to the floor which we calculate</param>
    /// <param name="maxChecks">How many z-levels down are we prepared to check? The default is 1, since in most cases we don't need to check more than that.</param>
    /// <returns></returns>
    public float DistanceToGround(Entity<CEZPhysicsComponent?> target, int maxChecks = 1)
    {
        if (!Resolve(target,
                ref target.Comp)) //maybe in future: simpler distance calculation for entities without zPhysComp?
            return maxChecks;

        var worldPos = _transform.GetGridOrMapTilePosition(target);

        var map = Transform(target).MapUid;
        if (!_gridQuery.TryComp(map, out var mapGrid))
            return maxChecks; //uhhh, ehhh, ok?

        //Мы сначала проверяем все прикрученные тайлы на текущем уровне, считая высоту. Если таких нет, и тайл пуст - мы проверяем уровень ниже, и ниже, и ниже...
        var zDistance = target.Comp.LocalPosition;

        for (var i = 0; i <= maxChecks; i++)
        {
            var checkingMapUid = map;
            var checkingMapComp = mapGrid;

            if (i != 0) //Map checking selection
            {
                zDistance++;
                if (!TryMapOffset(map.Value, -i, out _, out checkingMapUid))
                    continue;
                if (!_gridQuery.TryComp(checkingMapUid, out checkingMapComp))
                    continue;
            }

            //Check all types of ZHeight entities
            var query = _map.GetAnchoredEntitiesEnumerator(checkingMapUid.Value, checkingMapComp, worldPos);
            while (query.MoveNext(out var uid))
            {
                if (_supportQuery.TryComp(uid, out var support))
                {
                    return zDistance - support.Height;
                }
            }

            //No ZEntities found, check floor tiles
            if (_map.TryGetTileRef(checkingMapUid.Value, checkingMapComp, worldPos, out var tileRef) &&
                !tileRef.Tile.IsEmpty)
                return zDistance;
        }

        return maxChecks;
    }

    /// <summary>
    /// Checks whether there is a ceiling above the specified entity (tiles on the layer above).
    /// If there are no Z-levels above, false will be returned.
    /// </summary>
    public bool HasRoof(EntityUid target)
    {
        var mapUid = Transform(target).MapUid;

        if (mapUid is null)
            return false;

        if (!TryMapUp(mapUid.Value, out var mapAbove, out var mapAboveUid))
            return false;

        if (!_gridQuery.TryComp(mapAboveUid.Value, out var mapAboveGrid))
            return false;

        if (_map.TryGetTileRef(mapAboveUid.Value, mapAboveGrid, _transform.GetWorldPosition(target), out var tileRef) &&
            !tileRef.Tile.IsEmpty)
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

/// <summary>
/// It is called on an entity when it hits the floor or ceiling with force.
/// </summary>
/// <param name="impactPower">The speed at the moment of impact. Always positive</param>
public sealed class CEZLevelHitEvent(float impactPower) : EntityEventArgs
{
    public float ImpactPower = impactPower;
}
