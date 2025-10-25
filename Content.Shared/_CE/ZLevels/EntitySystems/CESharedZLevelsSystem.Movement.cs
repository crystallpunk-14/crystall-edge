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

            var grounded = HasGround(uid);

            var oldVelocity = zPhys.Velocity;
            var oldHeight = zPhys.LocalPosition;

            //Gravity force application
            ApplyZGravityForce(uid, zPhys, xform, physics, frameTime);

            //Movement application
            zPhys.LocalPosition += zPhys.Velocity * frameTime;
            if (zPhys.LocalPosition < 0) //Falling down
            {
                if (grounded)
                {
                    zPhys.LocalPosition = 0;

                    if (MathF.Abs(zPhys.Velocity) >= ImpactVelocityLimit)
                    {
                        RaiseLocalEvent(uid, new CEZLevelHitEvent(-zPhys.Velocity));
                        var land = new LandEvent(null, true);
                        RaiseLocalEvent(uid, ref land);
                    }

                    zPhys.Velocity = 0;
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
                    zPhys.LocalPosition = 1;

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
                        zPhys.LocalPosition -= 1;
                }
            }

            if (Math.Abs(oldVelocity - zPhys.Velocity) > 0.01f)
                DirtyField(uid, zPhys, nameof(CEZPhysicsComponent.Velocity));

            if (Math.Abs(oldHeight - zPhys.LocalPosition) > 0.01f)
                DirtyField(uid, zPhys, nameof(CEZPhysicsComponent.LocalPosition));
        }
    }

    private void ApplyZGravityForce(EntityUid uid, CEZPhysicsComponent zPhys, TransformComponent xform, PhysicsComponent physics, float frameTime)
    {
        if (physics.BodyStatus == BodyStatus.InAir)
            return;
        if (_ghostQuery.HasComp(uid))
            return;
        if (xform.ParentUid != xform.MapUid)
            return;

        if (zPhys.Velocity > 0)
            zPhys.Velocity -= ZGravityForce * frameTime * 0.5f; //Gamedesign hack: we have less gravity, when moveing up.
        else
            zPhys.Velocity -= ZGravityForce * frameTime;
    }

    /// <summary>
    /// Checks whether there is a floor under the feet of the specified entity (tiles at the same level, or anchored zLevelSupportComponent on level below).
    /// </summary>
    /// <param name="target"></param>
    /// <returns></returns>
    public bool HasGround(EntityUid target)
    {
        var map = Transform(target).MapUid;
        if (!_gridQuery.TryComp(map, out var mapGrid))
            return true; //uhhh, ehhh, ok?

        var worldPos = _transform.GetGridOrMapTilePosition(target);
        if (_map.TryGetTileRef(map.Value, mapGrid, worldPos, out var tileRef) && !tileRef.Tile.IsEmpty)
            return true;

        //Check for zLevelSupportComponent on the level below
        if (TryMapDown(map.Value, out var mapBelowId, out var mapBelowUid) && _gridQuery.TryComp(mapBelowUid, out var mapBelowComp))
        {
            var query = _map.GetAnchoredEntitiesEnumerator(mapBelowUid.Value, mapBelowComp, worldPos);
            while (query.MoveNext(out var uid))
            {
                if (_supportQuery.HasComp(uid))
                    return true;
            }
        }

        return false;
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

/// <summary>
/// It is called on an entity when it hits the floor or ceiling with force.
/// </summary>
/// <param name="impactPower">The speed at the moment of impact. Always positive</param>
public sealed class CEZLevelHitEvent(float impactPower) : EntityEventArgs
{
    public float ImpactPower = impactPower;
}
