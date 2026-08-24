using Content.Shared.Damage.Systems;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.EntityEffect.Effects;

/// <summary>
/// Restores the target's stamina.
/// </summary>
public sealed partial class StaminaRestore : CEEntityEffectBase<StaminaRestore>
{
    [DataField]
    public float Amount = 10f;

    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) =>
        Loc.GetString("ce-entity-effect-guidebook-stamina-restore", ("amount", Amount));
}

public sealed partial class CEStaminaRestoreEffectSystem : CEEntityEffectSystem<StaminaRestore>
{
    [Dependency] private SharedStaminaSystem _stamina = default!;

    protected override void Effect(ref CEEntityEffectEvent<StaminaRestore> args)
    {
        if (ResolveEffectEntity(args.Args, args.Effect.EffectTarget) is not { } entity)
            return;

        _stamina.TakeStaminaDamage(entity, -args.Effect.Amount * args.Args.Power);
    }
}