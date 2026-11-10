using Content.Shared.Body.Systems;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.EntityEffect.Effects;

/// <summary>
/// Restores or drains blood of the resolved target. Restoring never goes above the normal blood volume.
/// </summary>
public sealed partial class ModifyBloodLevel : CEEntityEffectBase<ModifyBloodLevel>
{
    [DataField(required: true)]
    public FixedPoint2 Amount;

    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return Loc.GetString(Amount < 0
                ? "ce-entity-effect-guidebook-modify-blood-level-drain"
                : "ce-entity-effect-guidebook-modify-blood-level-restore",
            ("amount", FixedPoint2.Abs(Amount).Float()));
    }
}

public sealed partial class CEModifyBloodLevelEffectSystem : CEEntityEffectSystem<ModifyBloodLevel>
{
    [Dependency] private BloodstreamSystem _bloodstream = default!;

    protected override void Effect(ref CEEntityEffectEvent<ModifyBloodLevel> args)
    {
        if (ResolveEffectEntity(args.Args, args.Effect.EffectTarget) is not { } entity)
            return;

        _bloodstream.TryModifyBloodLevel(entity, args.Effect.Amount * args.Args.Power);
    }
}
