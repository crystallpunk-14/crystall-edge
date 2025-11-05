using System.Numerics;
using Content.Shared.Chasm;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Throwing;
using JetBrains.Annotations;
using Robust.Shared.Audio;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;

namespace Content.Shared._CE.ZLevels.EntitySystems;

public abstract partial class CESharedZLevelsSystem
{
    public const int MaxZLevelsBelowRendering = 3;

    private const float ZGravityForce = 9.8f;
    private const float ZVelocityLimit = 20.0f;

    /// <summary>
    /// The maximum height at which a player will automatically climb higher when stepping on a highground entity.
    /// </summary>
    private const float MaxStepHeight = 0.5f;

    /// <summary>
    /// The minimum speed required to trigger LandEvent events.
    /// </summary>
    private const float ImpactVelocityLimit = 4.0f;

    private EntityQuery<CEZLevelHighGroundComponent> _highgroundQuery;

    private void InitMovement()
    {
        _highgroundQuery = GetEntityQuery<CEZLevelHighGroundComponent>();

        SubscribeLocalEvent<DamageableComponent, CEZLevelHitEvent>(OnFallDamage);
        SubscribeLocalEvent<PhysicsComponent, CEZLevelHitEvent>(OnFallAreaImpact);
    }

    private void OnFallDamage(Entity<DamageableComponent> ent, ref CEZLevelHitEvent args)
    {
        var knockdownTime = MathF.Min(args.ImpactPower * 0.25f, 5f);
        _stun.TryKnockdown(ent.Owner, TimeSpan.FromSeconds(knockdownTime));

        var damageType = _proto.Index<DamageTypePrototype>("Blunt");
        var damageAmount = MathF.Pow(args.ImpactPower, 2);

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

            var damageType = _proto.Index<DamageTypePrototype>("Blunt");
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
            if (!zPhys.Active)
                continue;

            if (physics.BodyType == BodyType.Static || xform.ParentUid != xform.MapUid)
                continue;

            var oldVelocity = zPhys.Velocity;
            var oldHeight = zPhys.LocalPosition;

            //Gravity force application
            if (physics.BodyStatus == BodyStatus.OnGround || zPhys.Velocity > 0)
                zPhys.Velocity -= ZGravityForce * frameTime;

            //Movement application
            zPhys.LocalPosition += zPhys.Velocity * frameTime;

            var distanceToGround = DistanceToGround((uid, zPhys), out var stickyGround);

            if ((distanceToGround <= 0.05f || stickyGround) && distanceToGround <= MaxStepHeight)
                zPhys.LocalPosition -= distanceToGround;
            if (distanceToGround <= 0.05f) //Theres a ground
            {
                if (MathF.Abs(zPhys.Velocity) >= ImpactVelocityLimit)
                {
                    RaiseLocalEvent(uid, new CEZLevelHitEvent(-zPhys.Velocity));
                    var land = new LandEvent(null, true);
                    RaiseLocalEvent(uid, ref land);
                }

                zPhys.Velocity = -zPhys.Velocity * zPhys.Bounciness;
            }

            if (zPhys.LocalPosition < 0) //We wanna fall down on ZLevel below
            {
                if (TryMoveDownOrChasm(uid))
                {
                    zPhys.LocalPosition += 1;

                    if (!stickyGround)
                    {
                        var fallEv = new CEZLevelFallEvent();
                        RaiseLocalEvent(uid, fallEv);
                    }
                }
            }
            else if (zPhys.LocalPosition >= 1) //Going up
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

    /// <summary>
    /// Returns the distance to the floor. Returns <see cref="maxFloors"/> if the distance is too great.
    /// </summary>
    /// <param name="target">The entity, the distance to the floor which we calculate</param>
    /// <param name="stickyGround">true in situations where the entity smoothly descends along a sticky diagonal descent like a staircase</param>
    /// <param name="maxFloors">How many z-levels down are we prepared to check? The default is 1, since in most cases we don't need to check more than that.</param>
    /// <returns></returns>
    public float DistanceToGround(Entity<CEZPhysicsComponent?> target, out bool stickyGround, int maxFloors = 1)
    {
        stickyGround = false;
        if (!Resolve(target,
                ref target.Comp,
                false)) //maybe in future: simpler distance calculation for entities without zPhysComp?
            return maxFloors;

        var xform = Transform(target);
        if (!_zMapQuery.TryComp(xform.MapUid, out var zMapComp))
            return maxFloors;
        if (!_gridQuery.TryComp(xform.MapUid, out var mapGrid))
            return maxFloors;

        var worldPosI = _transform.GetGridOrMapTilePosition(target);
        var worldPos = _transform.GetWorldPosition(target);

        //Select current map by default
        Entity<CEZLevelMapComponent> checkingMap = (xform.MapUid.Value, zMapComp);
        var checkingGrid = mapGrid;

        for (var floor = 0; floor <= maxFloors; floor++)
        {
            if (floor != 0) //Select map below
            {
                if (!TryMapOffset((checkingMap.Owner, checkingMap.Comp), -floor, out var tempCheckingMap))
                    continue;
                if (!_gridQuery.TryComp(tempCheckingMap, out var tempCheckingGrid))
                    continue;

                checkingMap = tempCheckingMap.Value;
                checkingGrid = tempCheckingGrid;
            }

            //Check all types of ZHeight entities
            var query = _map.GetAnchoredEntitiesEnumerator(checkingMap, checkingGrid, worldPosI);
            while (query.MoveNext(out var uid))
            {
                if (!_highgroundQuery.TryComp(uid, out var heightComp))
                    continue;

                var dir = _transform.GetWorldRotation(uid.Value).GetCardinalDir();

                var local = new Vector2((worldPos.X % 1 + 1) % 1, (worldPos.Y % 1 + 1) % 1);

                var t = dir switch
                {
                    Direction.East => heightComp.Corner ? (local.X + 1f - local.Y) / 2f : local.X,
                    Direction.West => heightComp.Corner ? (1f - local.X + local.Y) / 2f : 1f - local.X,
                    Direction.North => heightComp.Corner ? (local.X + local.Y) / 2f : local.Y,
                    Direction.South => heightComp.Corner ? (1f - local.X + 1f - local.Y) / 2f : 1f - local.Y,
                    _ => 0.5f,
                };

                t = Math.Clamp(t, 0f, 1f);

                var curve = heightComp.HeightCurve;
                if (curve.Count == 0)
                    continue;

                if (curve.Count == 1)
                    return target.Comp.LocalPosition + floor - curve[0];

                var step = 1f / (curve.Count - 1);
                var index = (int)(t / step);
                var frac = (t - index * step) / step;

                var y0 = curve[Math.Clamp(index, 0, curve.Count - 1)];
                var y1 = curve[Math.Clamp(index + 1, 0, curve.Count - 1)];

                var distance = target.Comp.LocalPosition + floor - MathHelper.Lerp(y0, y1, frac);

                if (target.Comp.Velocity < -0 && target.Comp.Velocity > -2 && heightComp.Stick)
                    stickyGround = true;

                return distance;
            }

            //No ZEntities found, check floor tiles
            if (_map.TryGetTileRef(checkingMap, checkingGrid, worldPosI, out var tileRef) &&
                !tileRef.Tile.IsEmpty)
                return target.Comp.LocalPosition + floor;
        }

        return maxFloors;
    }

    /// <summary>
    /// Checks whether there is a ceiling above the specified entity (tiles on the layer above).
    /// If there are no Z-levels above, false will be returned.
    /// </summary>
    [PublicAPI]
    public bool HasRoof(EntityUid ent, Entity<CEZLevelMapComponent?>? map = null)
    {
        map ??= Transform(ent).MapUid;

        if (map is null)
            return false;

        if (!TryMapUp(map.Value, out var mapAboveUid))
            return false;

        if (!_gridQuery.TryComp(mapAboveUid.Value, out var mapAboveGrid))
            return false;

        if (_map.TryGetTileRef(mapAboveUid.Value, mapAboveGrid, _transform.GetWorldPosition(ent), out var tileRef) &&
            !tileRef.Tile.IsEmpty)
            return true;

        return false;
    }

    /// <summary>
    /// Sets the vertical velocity for the entity. Positive values make the entity fly upward. Negative values make it fly downward.
    /// </summary>
    [PublicAPI]
    public void SetZVelocity(Entity<CEZPhysicsComponent?> ent, float newVelocity)
    {
        if (!Resolve(ent.Owner, ref ent.Comp))
            return;

        ent.Comp.Velocity = newVelocity;
        DirtyField(ent, ent.Comp, nameof(CEZPhysicsComponent.Velocity));
    }

    /// <summary>
    /// Add the vertical velocity for the entity. Positive values make the entity fly upward. Negative values make it fly downward.
    /// </summary>
    [PublicAPI]
    public void AddZVelocity(Entity<CEZPhysicsComponent?> ent, float newVelocity)
    {
        if (!Resolve(ent.Owner, ref ent.Comp, false))
            return;

        ent.Comp.Velocity += newVelocity;
        DirtyField(ent, ent.Comp, nameof(CEZPhysicsComponent.Velocity));
    }

    [PublicAPI]
    public bool TryMove(EntityUid ent, int offset, Entity<CEZLevelMapComponent?>? map = null)
    {
        map ??= Transform(ent).MapUid;

        if (map is null)
            return false;

        if (!TryMapOffset(map.Value, offset, out var targetMap))
            return false;

        if (!_mapQuery.TryComp(targetMap, out var targetMapComp))
            return false;


        _transform.SetMapCoordinates(ent, new MapCoordinates(_transform.GetWorldPosition(ent), targetMapComp.MapId));

        var ev = new CEZLevelMoveEvent(offset);
        RaiseLocalEvent(ent, ev);

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
/// Is called on an entity when it moves between z-levels.
/// </summary>
/// <param name="offset">How many levels were crossed. If negative, it means there was a downward movement. If positive, it means an upward movement.</param>
public sealed class CEZLevelMoveEvent(int offset) : EntityEventArgs
{
    public int Offset = offset;
}

/// <summary>
/// Is triggered when an entity falls to the lower z-levels under the force of gravity
/// </summary>
public sealed class CEZLevelFallEvent : EntityEventArgs;

/// <summary>
/// It is called on an entity when it hits the floor or ceiling with force.
/// </summary>
/// <param name="impactPower">The speed at the moment of impact. Always positive</param>
public sealed class CEZLevelHitEvent(float impactPower) : EntityEventArgs
{
    public float ImpactPower = impactPower;
}
