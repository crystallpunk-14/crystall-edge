using System;
using System.Numerics;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared._CE.EntityEffect.Effects;

public sealed partial class ShootProjectile : CEEntityEffectBase<ShootProjectile>
{
    [DataField(required: true)]
    public EntProtoId Prototype;

    [DataField]
    public float ProjectileSpeed = 20f;

    [DataField]
    public float? ProjectileMaxSpeed = null;

    [DataField]
    public float Spread;

    [DataField]
    public int ProjectileCount = 1;

    [DataField]
    public bool SaveVelocity;
}

public sealed partial class CEShootProjectileEffectSystem : CEEntityEffectSystem<ShootProjectile>
{
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SharedPhysicsSystem _physics = default!;
    [Dependency] private SharedGunSystem _gun = default!;
    [Dependency] private IMapManager _mapManager = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private IRobustRandom _random = default!;

    protected override void Effect(ref CEEntityEffectEvent<ShootProjectile> args)
    {
        if (!_net.IsServer)
            return;

        var xform = Transform(args.Args.Source);

        var fromCoords = xform.Coordinates;
        var userVelocity = _physics.GetMapLinearVelocity(args.Args.Source);

        // If applicable, this ensures the projectile is parented to grid on spawn, instead of the map.
        var fromMap = _transform.ToMapCoordinates(fromCoords);

        var spawnCoords = _mapManager.TryFindGridAt(fromMap, out var gridUid, out _)
            ? _transform.WithEntityId(fromCoords, gridUid)
            : new(_mapManager.GetMapEntityId(fromMap.MapId), fromMap.Position);

        // Resolve direction: prefer target coordinates, fall back to angle.
        var baseDirection = Vector2.Zero;
        if (TryResolveTargetCoordinates(args.Args, out var targetPoint))
        {
            baseDirection = _transform.ToMapCoordinates(targetPoint).Position -
                            _transform.ToMapCoordinates(spawnCoords).Position;
        }

        // Fall back to angle when no target or target is the user (zero direction).
        if (baseDirection == default)
        {
            baseDirection = args.Args.Angle.ToWorldVec();
        }

        var projCount = Math.Max(1, args.Effect.ProjectileCount);
        var baseAngle = MathF.Atan2(baseDirection.Y, baseDirection.X);

        for (var i = 0; i < projCount; i++)
        {
            // Interpret Spread as the angle (in radians) between adjacent projectiles.
            var center = (projCount - 1) / 2.0f;
            var angleOffset = (i - center) * args.Effect.Spread;
            var angle = baseAngle + angleOffset;

            var direction = new Vector2(MathF.Cos(angle), MathF.Sin(angle));

            if (direction == Vector2.Zero)
                continue;

            var ent = SpawnAtPosition(args.Effect.Prototype, spawnCoords);

            var speed = args.Effect.ProjectileSpeed;
            if (args.Effect.ProjectileMaxSpeed is { } max)
            {
                var min = MathF.Min(speed, max);
                var maxv = MathF.Max(speed, max);
                speed = _random.NextFloat(min, maxv);
            }

            _gun.ShootProjectile(ent,
                direction,
                args.Effect.SaveVelocity ? userVelocity : new Vector2(),
                args.Args.Source,
                args.Args.Source,
                speed);
        }
    }
}
