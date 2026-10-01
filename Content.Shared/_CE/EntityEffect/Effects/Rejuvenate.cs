using Content.Shared.Rejuvenate;

namespace Content.Shared._CE.EntityEffect.Effects;

/// <summary>
/// Fully restores the target: raises <see cref="RejuvenateEvent"/> on it.
/// </summary>
public sealed partial class Rejuvenate : CEEntityEffectBase<Rejuvenate>;

public sealed partial class CERejuvenateEffectSystem : CEEntityEffectSystem<Rejuvenate>
{
    protected override void Effect(ref CEEntityEffectEvent<Rejuvenate> args)
    {
        if (ResolveEffectEntity(args.Args, args.Effect.EffectTarget) is not { } entity)
            return;

        var ev = new RejuvenateEvent();
        RaiseLocalEvent(entity, ev);
    }
}
