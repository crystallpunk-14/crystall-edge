using Content.Server._CE.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Charges.Components;
using Content.Shared.Charges.Systems;
using Content.Shared.EntityConditions;
using Robust.Shared.Prototypes;

namespace Content.Server._CE.Production;

/// <summary>
/// Checks the standard charges of one uniquely granted action.
/// </summary>
public sealed partial class CEActionChargesCondition : EntityConditionBase<CEActionChargesCondition>
{
    [DataField(required: true)]
    public EntProtoId Action;

    [DataField]
    public int MinimumCharges = 1;

    public override string EntityConditionGuidebookText(IPrototypeManager prototype) => string.Empty;
}

public sealed partial class CEActionChargesConditionSystem
    : EntityConditionSystem<ActionsComponent, CEActionChargesCondition>
{
    [Dependency] private CEGrantedActionResolverSystem _actionResolver = default!;
    [Dependency] private SharedChargesSystem _charges = default!;

    [Dependency] private EntityQuery<LimitedChargesComponent> _limitedChargesQuery = default!;

    protected override void Condition(
        Entity<ActionsComponent> entity,
        ref EntityConditionEvent<CEActionChargesCondition> args)
    {
        if (args.Condition.MinimumCharges < 0 ||
            !_actionResolver.TryResolveUnique(entity.Owner, args.Condition.Action, out var action) ||
            !_limitedChargesQuery.TryComp(action, out var limitedCharges))
        {
            args.Result = false;
            return;
        }

        args.Result = _charges.HasCharges(
            (action.Owner, limitedCharges),
            args.Condition.MinimumCharges);
    }
}
