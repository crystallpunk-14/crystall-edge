using Content.Shared._CE.Farming;
using Content.Shared._CE.Farming.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.EntityEffect.Effects;

/// <summary>
/// Adds growth to a <see cref="CEPlantComponent"/>.
/// </summary>
public sealed partial class AffectPlantGrowth : CEEntityEffectBase<AffectPlantGrowth>
{
    [DataField(required: true)]
    public float Amount;

    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) =>
        Loc.GetString("ce-entity-effect-guidebook-affect-plant-growth", ("amount", Amount));
}

public sealed partial class CEAffectPlantGrowthEffectSystem : CEEntityEffectSystem<AffectPlantGrowth>
{
    [Dependency] private CESharedFarmingSystem _farming = default!;

    protected override void Effect(ref CEEntityEffectEvent<AffectPlantGrowth> args)
    {
        if (ResolveEffectEntity(args.Args, args.Effect.EffectTarget) is not { } entity)
            return;

        if (!TryComp<CEPlantComponent>(entity, out var plant))
            return;

        _farming.AffectGrowth((entity, plant), args.Effect.Amount * args.Args.Power);
    }
}