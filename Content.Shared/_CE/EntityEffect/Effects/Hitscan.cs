using Content.Shared.Weapons.Hitscan.Events;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.EntityEffect.Effects;

public sealed partial class Hitscan : CEEntityEffectBase<Hitscan>
{
    [DataField(required: true)]
    public EntProtoId Prototype;

    /// <summary>
    ///     Offset added to the resolved base direction (target coordinates, falling back to the effect args'
    ///     angle). Fire several copies of this effect with different offsets to recreate a spread/volley.
    /// </summary>
    [DataField]
    public Angle Angle;
}

public sealed partial class CEHitscanEffectSystem : CEEntityEffectSystem<Hitscan>
{
    [Dependency] private INetManager _net = default!;

    protected override void Effect(ref CEEntityEffectEvent<Hitscan> args)
    {
        if (!_net.IsServer)
            return;

        var fromCoords = Transform(args.Args.Source).Coordinates;
        var direction = (ResolveDirection(args.Args) + args.Effect.Angle).ToWorldVec();

        var hitscanEnt = SpawnAtPosition(args.Effect.Prototype, fromCoords);

        var hitscanEv = new HitscanTraceEvent
        {
            FromCoordinates = fromCoords,
            ShotDirection = direction,
            Gun = args.Args.Used ?? args.Args.Source,
            Shooter = args.Args.Source,
        };
        RaiseLocalEvent(hitscanEnt, ref hitscanEv);
    }
}
