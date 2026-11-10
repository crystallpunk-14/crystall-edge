namespace Content.Shared._CE.EntityEffect.Conditions;

/// <summary>
/// Passes when at least one of the nested conditions passes.
/// </summary>
public sealed partial class AnyOf : CEEntityConditionBase<AnyOf>
{
    [DataField(required: true)]
    public List<CEEntityCondition> Conditions = new();
}

public sealed partial class CEAnyOfConditionSystem : CEEntityConditionSystem<AnyOf>
{
    protected override void Condition(ref CEEntityConditionEvent<AnyOf> args)
    {
        foreach (var condition in args.Condition.Conditions)
        {
            if (!condition.Passes(args.Args))
                continue;

            args.Result = true;
            return;
        }
    }
}
