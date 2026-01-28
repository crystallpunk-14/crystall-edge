using Content.Shared.Stealth;

namespace Content.Shared._CE.Actions.Spells;

public sealed partial class CESpellRevealStealthUser : CESpellEffect
{
    public override void Effect(EntityManager entManager, CESpellEffectBaseArgs args)
    {
        if (args.User is null)
            return;

        var stealth = entManager.System<SharedStealthSystem>();

        stealth.SetVisibility(args.User.Value, 1);
    }
}