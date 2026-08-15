using System.Numerics;
using Content.Shared.Weapons.Ranged.Systems;
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
    public float? ProjectileMaxSpeed;

    /// <summary>
    ///     Offset added to the resolved base direction (target coordinates, falling back to the effect args'
    ///     angle). Fire several copies of this effect with different offsets to recreate a spread/volley.
    /// </summary>
    [DataField]
    public Angle Angle;

    [DataField]
    public bool SaveVelocity;

    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        var itemName = prototype.TryIndex(Prototype, out var itemProto) ? itemProto.Name : Prototype.Id;
        return Loc.GetString("ce-entity-effect-guidebook-shoot-projectile", ("item", itemName));
    }
}

public sealed partial class CEShootProjectileEffectSystem : CEEntityEffectSystem<ShootProjectile>
{
    [Dependency] private SharedPhysicsSystem _physics = default!;
    [Dependency] private SharedGunSystem _gun = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private IRobustRandom _random = default!;

    protected override void Effect(ref CEEntityEffectEvent<ShootProjectile> args)
    {
        if (!_net.IsServer)
            return;

        var fromCoords = Transform(args.Args.Source).Coordinates;
        var direction = (ResolveDirection(args.Args) + args.Effect.Angle).ToWorldVec();

        var speed = args.Effect.ProjectileSpeed;
        if (args.Effect.ProjectileMaxSpeed is { } max)
        {
            var min = MathF.Min(speed, max);
            var maxSpeed = MathF.Max(speed, max);
            speed = _random.NextFloat(min, maxSpeed);
        }

        var userVelocity = args.Effect.SaveVelocity ? _physics.GetMapLinearVelocity(args.Args.Source) : new Vector2();

        var ent = SpawnAtPosition(args.Effect.Prototype, fromCoords);
        _gun.ShootProjectile(ent, direction, userVelocity, args.Args.Source, args.Args.Source, speed);
    }
}
