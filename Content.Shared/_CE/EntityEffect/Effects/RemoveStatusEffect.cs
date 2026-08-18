using Content.Shared.StatusEffectNew;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.EntityEffect.Effects;

/// <summary>
/// Fully removes a status effect from the target entity, regardless of stacks.
/// </summary>
public sealed partial class RemoveStatusEffect : CEEntityEffectBase<RemoveStatusEffect>
{
    [DataField(required: true)]
    public EntProtoId StatusEffect;

    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        var statusName = prototype.TryIndex(StatusEffect, out var statusProto) ? statusProto.Name : StatusEffect.Id;
        return Loc.GetString("ce-entity-effect-guidebook-remove-status", ("status", statusName));
    }
}

public sealed partial class CERemoveStatusEffectSystem : CEEntityEffectSystem<RemoveStatusEffect>
{
    [Dependency] private StatusEffectsSystem _statusEffect = default!;

    protected override void Effect(ref CEEntityEffectEvent<RemoveStatusEffect> args)
    {
        if (ResolveEffectEntity(args.Args, args.Effect.EffectTarget) is not { } entity)
            return;

        _statusEffect.TryRemoveStatusEffect(entity, args.Effect.StatusEffect);
    }
}
