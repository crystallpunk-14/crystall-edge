using Content.Server.Polymorph.Systems;
using Content.Shared._CE.EntityEffect;
using Content.Shared._CE.EntityEffect.Effects;

namespace Content.Server._CE.EntityEffect.Effects;

public sealed partial class CEPolymorphEffectSystem : CEEntityEffectSystem<CEPolymorph>
{
    [Dependency] private PolymorphSystem _polymorph = default!;

    protected override void Effect(ref CEEntityEffectEvent<CEPolymorph> args)
    {
        if (ResolveEffectEntity(args.Args, args.Effect.EffectTarget) is not { } entity)
            return;

        _polymorph.PolymorphEntity(entity, args.Effect.PolymorphPrototype);
    }
}
