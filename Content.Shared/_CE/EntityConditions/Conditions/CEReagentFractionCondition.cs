using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.EntityConditions;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._CE.EntityConditions.Conditions;

/// <summary>
/// Checks how much of a solution's current volume belongs to a reagent prototype.
/// </summary>
public sealed partial class CEReagentFractionEntityConditionSystem
    : EntityConditionSystem<SolutionComponent, CEReagentFractionCondition>
{
    protected override void Condition(
        Entity<SolutionComponent> entity,
        ref EntityConditionEvent<CEReagentFractionCondition> args)
    {
        args.Result = false;

        var condition = args.Condition;
        var solution = entity.Comp.Solution;
        if (!float.IsFinite(condition.MinFraction) ||
            !float.IsFinite(condition.MaxFraction) ||
            condition.MinFraction < 0f ||
            condition.MaxFraction > 1f ||
            condition.MinFraction > condition.MaxFraction ||
            solution.Volume <= 0)
        {
            return;
        }

        var reagentVolume = solution.GetTotalPrototypeQuantity(condition.Reagent);
        var fraction = reagentVolume.Float() / solution.Volume.Float();
        args.Result = fraction >= condition.MinFraction && fraction <= condition.MaxFraction;
    }
}

/// <summary>
/// A reusable solution-composition condition. Fractions are inclusive and use the current
/// solution volume rather than the container capacity.
/// </summary>
[SerializedType("CEReagentFractionCondition")]
public sealed partial class CEReagentFractionCondition : EntityConditionBase<CEReagentFractionCondition>
{
    [DataField(required: true)]
    public ProtoId<ReagentPrototype> Reagent;

    [DataField(required: true)]
    public float MinFraction;

    [DataField]
    public float MaxFraction = 1f;

    public override string EntityConditionGuidebookText(IPrototypeManager prototype) => string.Empty;
}
