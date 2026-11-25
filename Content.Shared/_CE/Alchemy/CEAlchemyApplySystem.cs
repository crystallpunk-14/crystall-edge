using Content.Shared._CE.Alchemy.Data;
using Content.Shared._CE.Alchemy.World;
using Content.Shared._CE.EntityEffect;

namespace Content.Shared._CE.Alchemy;

/// <summary>
/// Metabolism effect of the alchemy mixture: analyzes the recipe carried by the reagent data and applies
/// the effects of every zone it hit, with the zone strength multiplied by the metabolism scale.
/// </summary>
public sealed partial class CEAlchemyApply : CEEntityEffectBase<CEAlchemyApply>;

public sealed partial class CEAlchemyApplySystem : CEEntityEffectSystem<CEAlchemyApply>
{
    [Dependency] private CEAlchemySystem _alchemy = default!;

    protected override void Effect(ref CEEntityEffectEvent<CEAlchemyApply> args)
    {
        if (args.Args.Data is not { } dataList)
            return;

        foreach (var data in dataList)
        {
            if (data is not CEAlchemyData alchemy)
                continue;

            foreach (var hit in _alchemy.Analyze(alchemy))
            {
                var zoneArgs = args.Args with { Power = hit.Strength * args.Args.Power, Data = null };
                foreach (var effect in hit.Zone.Effects)
                {
                    effect.Effect(zoneArgs);
                }
            }
        }
    }
}
