using Content.Shared.Stunnable;

namespace Content.Shared._CE.Actions.Spells;

public sealed partial class CESpellKnockdown : CESpellEffect
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

        var targetEntity = args.Target.Value;

        var stun = entManager.System<SharedStunSystem>();

        stun.TryKnockdown(args.Target.Value, Time, true, true, DropItems);
    }
}