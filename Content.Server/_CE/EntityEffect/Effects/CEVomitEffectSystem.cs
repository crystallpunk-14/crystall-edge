using Content.Shared._CE.EntityEffect;
using Content.Shared._CE.EntityEffect.Effects;
using Content.Shared.Medical;
using Robust.Shared.Random;

namespace Content.Server._CE.EntityEffect.Effects;

public sealed partial class CEVomitEffectSystem : CEEntityEffectSystem<Vomit>
{
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private VomitSystem _vomit = default!;

    protected override void Effect(ref CEEntityEffectEvent<Vomit> args)
    {
        if (ResolveEffectEntity(args.Args, args.Effect.EffectTarget) is not { } entity)
            return;

        if (!_random.Prob(args.Effect.Probability))
            return;

        _vomit.Vomit(entity, args.Effect.ThirstAmount, args.Effect.HungerAmount);
    }
}
