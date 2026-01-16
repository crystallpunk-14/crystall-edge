using Content.Shared._CE.Actions.Spells;
using Content.Shared._CE.Vampire.Components;
using Content.Shared.FixedPoint;

namespace Content.Shared._CE.Vampire.Spells;

public sealed partial class CESpellVampireGatherEssence : CESpellEffect
{
    [DataField]
    public FixedPoint2 Amount = 0.2f;

    public override void Effect(EntityManager entManager, CESpellEffectBaseArgs args)
    {
        if (args.Target is null)
            return;

        if (args.User is null)
            return;

        if (entManager.HasComponent<CEVampireComponent>(args.Target.Value))
            return;

        if (!entManager.TryGetComponent<CEVampireEssenceHolderComponent>(args.Target.Value, out var essenceHolder))
            return;

        var vamp = entManager.System<CESharedVampireSystem>();
        vamp.GatherEssence(args.User.Value, args.Target.Value, Amount);
    }
}
