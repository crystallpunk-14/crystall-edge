using Robust.Shared.Prototypes;

namespace Content.Shared._CE.EntityEffect.Conditions;

/// <summary>
/// Passes when the entity has all of <see cref="Components"/>, or any of them when <see cref="Any"/> is set.
/// </summary>
public sealed partial class HasComponents : CEEntityConditionBase<HasComponents>
{
    [DataField(required: true)]
    public ComponentRegistry Components = new();

    [DataField]
    public bool Any;
}

public sealed partial class CEHasComponentsConditionSystem : CEEntityConditionSystem<HasComponents>
{
    protected override void Condition(ref CEEntityConditionEvent<HasComponents> args)
    {
        foreach (var registration in args.Condition.Components.Values)
        {
            var has = HasComp(args.Entity, registration.Component.GetType());
            if (has == args.Condition.Any)
            {
                args.Result = has;
                return;
            }
        }

        args.Result = !args.Condition.Any;
    }
}
