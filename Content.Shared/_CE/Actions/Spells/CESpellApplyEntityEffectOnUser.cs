using Content.Shared.EntityEffects;

namespace Content.Shared._CE.Actions.Spells;

public sealed partial class CESpellApplyEntityEffectOnUser : CESpellEffect
{
    [DataField(required: true, serverOnly: true)]
    public List<EntityEffect> Effects = new();

    public override void Effect(EntityManager entManager, CESpellEffectBaseArgs args)
    {
        if (args.User == null)
            return;

        var effectSys = entManager.System<SharedEntityEffectsSystem>();

        var targetEntity = args.User.Value;

        effectSys.ApplyEffects(targetEntity, Effects.ToArray(), user: args.User);
    }
}
