using Content.Shared.EntityConditions;
using Content.Shared.Nutrition.Components;
using Robust.Shared.Prototypes;

namespace Content.Server._CE.EntityConditions;

/// <summary>
/// Checks whether canonical vanilla thirst is inside an inclusive range.
/// </summary>
public sealed partial class CEThirstCondition : EntityConditionBase<CEThirstCondition>
{
    [DataField]
    public float Min;

    [DataField]
    public float Max = float.PositiveInfinity;

    public override string EntityConditionGuidebookText(IPrototypeManager prototype) => string.Empty;
}

public sealed partial class CEThirstConditionSystem
    : EntityConditionSystem<ThirstComponent, CEThirstCondition>
{
    protected override void Condition(
        Entity<ThirstComponent> entity,
        ref EntityConditionEvent<CEThirstCondition> args)
    {
        args.Result = float.IsFinite(entity.Comp.CurrentThirst) &&
            entity.Comp.CurrentThirst >= args.Condition.Min &&
            entity.Comp.CurrentThirst <= args.Condition.Max;
    }
}
