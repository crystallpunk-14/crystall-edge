using System.Numerics;
using Content.Shared.Weapons.Hitscan.Events;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.EntityEffect.Effects;

public sealed partial class Hitscan : CEEntityEffectBase<Hitscan>
{
    [DataField(required: true)]
    public EntProtoId Prototype;

    [DataField]
    public float Spread;

    [DataField]
    public int ProjectileCount = 1;

    //public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    //{
    //    var itemName = prototype.TryIndex(Prototype, out var itemProto) ? itemProto.Name : Prototype.Id;
    //    return Loc.GetString("ce-entity-effect-guidebook-shoot-projectile", ("count", ProjectileCount), ("item", itemName));
    //}
}

public sealed partial class CEHitscanEffectSystem : CEEntityEffectSystem<Hitscan>
{
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private IMapManager _mapManager = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private INetManager _net = default!;

    protected override void Effect(ref CEEntityEffectEvent<Hitscan> args)
    {
        if (!_net.IsServer)
            return;

        var xform = Transform(args.Args.Source);

        var fromCoords = xform.Coordinates;
        var fromMap = _transform.ToMapCoordinates(fromCoords);

        var spawnCoords = _mapManager.TryFindGridAt(fromMap, out var gridUid, out _)
            ? _transform.WithEntityId(fromCoords, gridUid)
            : new EntityCoordinates(_map.GetMapOrInvalid(fromMap.MapId), fromMap.Position);

        // Resolve direction: prefer target coordinates, fall back to angle.
        var baseDirection = Vector2.Zero;
        if (TryResolveTargetCoordinates(args.Args, out var targetPoint))
        {
            baseDirection = _transform.ToMapCoordinates(targetPoint).Position -
                            _transform.ToMapCoordinates(spawnCoords).Position;
        }

        // Fall back to angle when no target or target is the user (zero direction).
        if (baseDirection == default)
            baseDirection = args.Args.Angle.ToWorldVec();

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

            var hitscanEnt = SpawnAtPosition(args.Effect.Prototype, spawnCoords);

            var hitscanEv = new HitscanTraceEvent
            {
                FromCoordinates = fromCoords,
                ShotDirection = direction,
                Gun = args.Args.Source,
                Shooter = args.Args.Source
            };
            RaiseLocalEvent(hitscanEnt, ref hitscanEv);
        }
    }
}
