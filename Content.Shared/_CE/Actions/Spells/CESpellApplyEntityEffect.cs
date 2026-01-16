using Content.Shared.EntityEffects;

namespace Content.Shared._CE.Actions.Spells;

public sealed partial class CESpellApplyEntityEffect : CESpellEffect
{
    [DataField(required: true, serverOnly: true)]
    public List<EntityEffect> Effects = new();

    public override void Effect(EntityManager entManager, CESpellEffectBaseArgs args)
    {
        if (args.Target is null)
            return;

        var effectSys = entManager.System<SharedEntityEffectsSystem>();

        var targetEntity = args.Target.Value;

        effectSys.ApplyEffects(targetEntity, Effects.ToArray(), user: args.User);
    }
}
