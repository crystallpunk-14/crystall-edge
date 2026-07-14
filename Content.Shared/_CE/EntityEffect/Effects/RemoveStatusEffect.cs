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
