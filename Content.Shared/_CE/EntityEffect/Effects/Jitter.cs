using Content.Shared.Jittering;

namespace Content.Shared._CE.EntityEffect.Effects;

/// <summary>
/// Applies a jitter animation to the effect entity via <see cref="SharedJitteringSystem"/>.
/// </summary>
public sealed partial class Jitter : CEEntityEffectBase<Jitter>
{
    [DataField]
    public TimeSpan Duration = TimeSpan.FromSeconds(2);

    [DataField]
    public float Amplitude = 10f;

    [DataField]
    public float Frequency = 4f;
}

public sealed partial class CEJitterEffectSystem : CEEntityEffectSystem<Jitter>
{
    [Dependency] private SharedJitteringSystem _jittering = default!;

    protected override void Effect(ref CEEntityEffectEvent<Jitter> args)
    {
        if (ResolveEffectEntity(args.Args, args.Effect.EffectTarget) is not { } entity)
            return;

        _jittering.DoJitter(entity, args.Effect.Duration, refresh: true, args.Effect.Amplitude, args.Effect.Frequency);
    }
}
