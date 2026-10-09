using Content.Server._CE.RestorationRitual;
using Content.Shared._CE.EntityEffect;
using Content.Shared._CE.EntityEffect.Effects;

namespace Content.Server._CE.EntityEffect.Effects;

public sealed partial class CEStartRestorationRitualEffectSystem : CEEntityEffectSystem<StartRestorationRitual>
{
    [Dependency] private CERestorationRitualSystem _ritual = default!;

    protected override void Effect(ref CEEntityEffectEvent<StartRestorationRitual> args)
    {
        if (ResolveEffectEntity(args.Args, args.Effect.EffectTarget) is not { } target)
            return;

        _ritual.TryStartRitual(args.Args.Source, target);
    }
}
