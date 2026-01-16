using Content.Shared._CE.Actions.Spells;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.FixedPoint;

namespace Content.Shared._CE.Vampire.Spells;

public sealed partial class CESpellSuckBlood : CESpellEffect
{
    [DataField]
    public FixedPoint2 SuckAmount = 10;
    public override void Effect(EntityManager entManager, CESpellEffectBaseArgs args)
    {
        if (args.Target is null)
            return;

        if (args.User is null)
            return;

        var solutionContainerSystem = entManager.System<SharedSolutionContainerSystem>();

        if (!solutionContainerSystem.TryGetSolution(args.Target.Value, "bloodstream", out var targetBloodstreamSolution))
            return;

        if (!solutionContainerSystem.TryGetSolution(args.User.Value, "chemicals", out var userSolution))
            return;

        solutionContainerSystem.TryTransferSolution(userSolution.Value,
            targetBloodstreamSolution.Value.Comp.Solution,
            SuckAmount);
    }
}
