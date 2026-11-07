using Content.Shared.Cuffs;
using Content.Shared.Cuffs.Components;

namespace Content.Shared._CE.EntityEffect.Effects;

/// <summary>
/// Removes all cuffs from the resolved entity and drops them at its feet.
/// </summary>
public sealed partial class Uncuff : CEEntityEffectBase<Uncuff>;

public sealed partial class CEUncuffEffectSystem : CEEntityEffectSystem<Uncuff>
{
    [Dependency] private SharedCuffableSystem _cuffable = default!;

    protected override void Effect(ref CEEntityEffectEvent<Uncuff> args)
    {
        if (ResolveEffectEntity(args.Args, args.Effect.EffectTarget) is not { } entity)
            return;

        if (!TryComp<CuffableComponent>(entity, out var cuffable))
            return;

        // Copy: Uncuff removes from the container we iterate
        var cuffs = new List<EntityUid>(_cuffable.GetAllCuffs((entity, cuffable)));
        foreach (var cuff in cuffs)
        {
            _cuffable.Uncuff(entity, null, cuff, cuffable);
        }
    }
}
