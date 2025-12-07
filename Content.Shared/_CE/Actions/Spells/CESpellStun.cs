using Content.Shared.Stunnable;
using Content.Shared.Trigger.Systems;

namespace Content.Shared._CE.Actions.Spells;

public sealed partial class CESpellStun: CESpellEffect
{
    [DataField(required: true)]
    public TimeSpan Duration = TimeSpan.FromSeconds(1f);

    [DataField]
    public bool DropItems = false;

    public override void Effect(EntityManager entManager, CESpellEffectBaseArgs args)
    {
        if (args.Target is null)
            return;

        var stun = entManager.System<SharedStunSystem>();

        stun.TryKnockdown(args.Target.Value, Duration, drop: DropItems);
        stun.TryAddStunDuration(args.Target.Value, Duration);
    }
}
