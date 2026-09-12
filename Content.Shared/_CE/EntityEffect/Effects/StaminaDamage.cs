using Content.Shared.Damage.Systems;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.EntityEffect.Effects;

/// <summary>
/// Deals stamina damage to the target.
/// </summary>
public sealed partial class StaminaDamage : CEEntityEffectBase<StaminaDamage>
{
    [DataField]
    public float Amount = 10f;

    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) =>
        Loc.GetString("ce-entity-effect-guidebook-stamina-damage", ("amount", Amount));
}

public sealed partial class CEStaminaDamageEffectSystem : CEEntityEffectSystem<StaminaDamage>
{
    [Dependency] private SharedStaminaSystem _stamina = default!;

    protected override void Effect(ref CEEntityEffectEvent<StaminaDamage> args)
    {
        if (ResolveEffectEntity(args.Args, args.Effect.EffectTarget) is not { } entity)
            return;

        _stamina.TakeStaminaDamage(entity, args.Effect.Amount * args.Args.Power);
    }
}