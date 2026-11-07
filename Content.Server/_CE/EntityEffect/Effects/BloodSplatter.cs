using Content.Server._CE.Footprints;
using Content.Shared._CE.EntityEffect;
using Content.Shared._CE.EntityEffect.Effects;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server._CE.EntityEffect.Effects;

public sealed partial class CEBloodSplatterEffectSystem : CEEntityEffectSystem<BloodSplatter>
{
    [Dependency] private CEFootprintSystem _footprint = default!;
    [Dependency] private IRobustRandom _random = default!;

    protected override void Effect(ref CEEntityEffectEvent<BloodSplatter> args)
    {
        var effect = args.Effect;
        if (effect.States.Count == 0 || ResolveEffectEntity(args.Args, effect.EffectTarget) is not { } target)
            return;

        var direction = args.Args.Angle;
        var spread = (float) effect.Spread.Theta;
        if (spread > 0f)
            direction += _random.NextFloat(-spread, spread);

        var distance = effect.MaxDistance > effect.MinDistance
            ? _random.NextFloat(effect.MinDistance, effect.MaxDistance)
            : effect.MinDistance;

        var rotation = effect.Rotation switch
        {
            CEDecalRotation.Random => _random.NextAngle(),
            CEDecalRotation.Direction => direction,
            _ => Angle.Zero,
        };

        var sprite = new SpriteSpecifier.Rsi(effect.Sprite, _random.Pick(effect.States));
        _footprint.TrySplatterBlood(target, direction, distance, effect.Volume, effect.Print, sprite, rotation, effect.MaxAlpha, effect.Vfx, effect.VfxOffset);
    }
}
