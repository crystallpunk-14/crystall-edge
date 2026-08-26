using Content.Shared._CE.Farming;
using Content.Shared._CE.Farming.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.EntityEffect.Effects;

/// <summary>
/// Adds energy to a <see cref="CEPlantComponent"/>.
/// </summary>
public sealed partial class AffectPlantEnergy : CEEntityEffectBase<AffectPlantEnergy>
{
    [DataField(required: true)]
    public float Amount;

    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) =>
        Loc.GetString("ce-entity-effect-guidebook-affect-plant-energy", ("amount", Amount));
}

public sealed partial class CEAffectPlantEnergyEffectSystem : CEEntityEffectSystem<AffectPlantEnergy>
{
    [Dependency] private CESharedFarmingSystem _farming = default!;

    protected override void Effect(ref CEEntityEffectEvent<AffectPlantEnergy> args)
    {
        if (ResolveEffectEntity(args.Args, args.Effect.EffectTarget) is not { } entity)
            return;

        if (!TryComp<CEPlantComponent>(entity, out var plant))
            return;

        _farming.AffectEnergy((entity, plant), args.Effect.Amount * args.Args.Power);
    }
}