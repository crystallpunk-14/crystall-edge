using Robust.Shared.Random;

namespace Content.Shared._CE.EntityEffect.Effects;

/// <summary>
/// Runs <see cref="Effects"/> once per ray of an evenly spaced star. Each run gets the ray's world angle as
/// <see cref="CEEntityEffectArgs.Angle"/>, so angle-aware effects (<see cref="SpawnDecal"/>, <see cref="SpawnEntity"/>
/// with <c>rotate</c>, projectiles) fan out around the target.
/// </summary>
public sealed partial class Radial : CEEntityEffectBase<Radial>
{
    [DataField(required: true)]
    public List<CEEntityEffect> Effects = new();

    [DataField]
    public int Count = 8;

    /// <summary>
    /// Turns the whole star by a random angle each time, so repeated runs don't line up.
    /// </summary>
    [DataField]
    public bool RandomStart = true;

    /// <summary>
    /// Random deviation of every ray, up to this angle either way.
    /// </summary>
    [DataField]
    public Angle Spread = Angle.Zero;
}

public sealed partial class CERadialEffectSystem : CEEntityEffectSystem<Radial>
{
    [Dependency] private IRobustRandom _random = default!;

    protected override void Effect(ref CEEntityEffectEvent<Radial> args)
    {
        var effect = args.Effect;
        if (effect.Count <= 0)
            return;

        var start = effect.RandomStart ? _random.NextAngle() : args.Args.Angle;
        var step = Math.Tau / effect.Count;
        var spread = (float) effect.Spread.Theta;

        for (var i = 0; i < effect.Count; i++)
        {
            var angle = start + step * i;
            if (spread > 0f)
                angle += _random.NextFloat(-spread, spread);

            var rayArgs = args.Args with { Angle = angle };
            foreach (var nested in effect.Effects)
            {
                nested.Effect(rayArgs);
            }
        }
    }
}
