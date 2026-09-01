using Content.Server._CE.Actions;
using Content.Server._CE.GOAP.Navigation;
using Content.Shared._CE.GOAP;
using Content.Shared.Actions;

namespace Content.Server._CE.GOAP.Actions;

/// <summary>
/// Converts the synchronous result of an existing GOAP action invocation into
/// the GOAP status and, for opt-in selectors, a temporary target exclusion.
/// The underlying action system remains the sole owner of validation and side effects.
/// </summary>
public sealed partial class CEGOAPUseActionResultAdapterSystem : EntitySystem
{
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private CEGrantedActionResolverSystem _resolver = default!;
    [Dependency] private CEGOAPTargetBackoffSystem _backoff = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CEGOAPTargetBackoffComponent, CEGOAPActionUpdateEvent<CEGOAPUseAction>>(
            OnUseActionUpdate,
            after: [typeof(CEGOAPUseActionSystem)]);
    }

    private void OnUseActionUpdate(
        Entity<CEGOAPTargetBackoffComponent> ent,
        ref CEGOAPActionUpdateEvent<CEGOAPUseAction> args)
    {
        if (args.Status != CEGOAPActionStatus.Finished)
            return;

        // The existing executor chooses the first matching granted action. Use
        // the same identity instead of imposing a different uniqueness policy
        // after side effects may already have occurred.
        if (!_resolver.TryResolveFirst(ent.Owner, args.Action.ActionPrototype, out var action))
            return;

        // SharedActionsSystem resets Handled immediately before raising the
        // synchronous action event. A false value here belongs to this attempt,
        // not to an earlier invocation.
        var actionEvent = _actions.GetEvent(action.Owner);
        if (actionEvent?.Handled == true)
            return;

        if (args.Action.Selector is ICEGOAPTargetBackoffSelector &&
            TryGetTarget(actionEvent, out var target))
        {
            _backoff.Reject(ent.Owner, target);
        }

        args.Status = CEGOAPActionStatus.Failed;
    }

    private static bool TryGetTarget(BaseActionEvent? actionEvent, out EntityUid target)
    {
        target = actionEvent switch
        {
            EntityTargetActionEvent entityTarget => entityTarget.Target,
            WorldTargetActionEvent { Entity: { } entity } => entity,
            _ => default,
        };

        return target.IsValid();
    }
}
