using Content.Shared.StatusEffectNew;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Actions.Spells;

public sealed partial class CESpellApplyStatusEffect : CESpellEffect
{
    [DataField(required: true)]
    public EntProtoId StatusEffect = default;

    [DataField(required: true)]
    public TimeSpan Duration = TimeSpan.FromSeconds(1f);

    [DataField]
    public bool Refresh = true;

    public override void Effect(EntityManager entManager, CESpellEffectBaseArgs args)
    {
        if (args.Target is null)
            return;

        var effectSys = entManager.System<StatusEffectsSystem>();

        if (!Refresh)
            effectSys.TryAddStatusEffectDuration(args.Target.Value, StatusEffect, Duration);
        else
            effectSys.TrySetStatusEffectDuration(args.Target.Value, StatusEffect, Duration);
    }
}
