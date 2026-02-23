using Content.Shared._CE.Actions.Spells;
using Content.Shared.Stunnable;

namespace Content.Shared._White.Actions.Spells;

public sealed partial class WhiteSpellKnockdown : CESpellEffect
{
    [DataField]
    public float ThrowPower = 10f;

    [DataField]
    public TimeSpan Time = TimeSpan.FromSeconds(1f);

    [DataField]
    public bool DropItems = false;

    public override void Effect(EntityManager entManager, CESpellEffectBaseArgs args)
    {
        if (args.Target is null || args.User is null)
            return;

        var stun = entManager.System<SharedStunSystem>();

        stun.TryKnockdown(args.Target.Value, Time, true, true, DropItems);
    }
}
