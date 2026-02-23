using Content.Shared._CE.Actions.Spells;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Prototypes;

namespace Content.Shared._White.Actions.Spells;

public sealed partial class WhiteSpellToggleStatusEffect : CESpellEffect
{
    [DataField(required: true)]
    public EntProtoId StatusEffect;

    public override void Effect(EntityManager entManager, CESpellEffectBaseArgs args)
    {
        if (args.Target is null)
            return;

        var effectSys = entManager.System<StatusEffectsSystem>();

        if (!effectSys.HasStatusEffect(args.Target.Value, StatusEffect))
            effectSys.TrySetStatusEffectDuration(args.Target.Value, StatusEffect);
        else
            effectSys.TryRemoveStatusEffect(args.Target.Value, StatusEffect);
    }
}
