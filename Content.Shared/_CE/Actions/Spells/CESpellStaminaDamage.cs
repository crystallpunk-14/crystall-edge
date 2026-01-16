using Content.Shared.Damage.Systems;

namespace Content.Shared._CE.Actions.Spells;

public sealed partial class CESpellStaminaDamage : CESpellEffect
{
    [DataField]
    public float Damage = 10f;

    public override void Effect(EntityManager entManager, CESpellEffectBaseArgs args)
    {
        if (args.Target is null)
            return;

        var stamina = entManager.System<SharedStaminaSystem>();

        stamina.TakeStaminaDamage(args.Target.Value, Damage, null, args.User, args.Used);
    }
}
