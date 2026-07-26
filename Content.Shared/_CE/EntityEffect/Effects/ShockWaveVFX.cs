using Content.Shared._CE.ShockWave;
using Robust.Shared.Network;
using Robust.Shared.Spawners;

namespace Content.Shared._CE.EntityEffect.Effects;

public sealed partial class ShockWaveVFX : CEEntityEffectBase<ShockWaveVFX>
{
    [DataField]
    public float Sharpness = 10;

    [DataField]
    public float Width = 0.8f;

    [DataField]
    public float FalloffPower = 40f;
}

public sealed partial class CEShockWaveVFXEffectSystem : CEEntityEffectSystem<ShockWaveVFX>
{
    [Dependency] private INetManager _net = default!;

    protected override void Effect(ref CEEntityEffectEvent<ShockWaveVFX> args)
    {
        if (!TryResolveEffectCoordinates(args.Args, args.Effect.EffectTarget, out var coords))
            return;

        if (_net.IsClient)
            return;

        var vfx = Spawn(null, coords);
        var shockWave = EnsureComp<CEShockWaveComponent>(vfx);

        shockWave.Sharpness = args.Effect.Sharpness;
        shockWave.Width = args.Effect.Width;
        shockWave.FalloffPower = args.Effect.FalloffPower;
        shockWave.Duration = 1;
        Dirty(vfx, shockWave);

        EnsureComp<TimedDespawnComponent>(vfx);
    }
}
