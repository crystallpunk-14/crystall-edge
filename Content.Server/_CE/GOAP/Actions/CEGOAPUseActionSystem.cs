using Content.Shared._CE.GOAP;
using Content.Shared._CE.GOAP.Components;
using Content.Shared._CE.GOAP.Selectors;
using Content.Shared.ActionBlocker;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Actions.Events;
using Content.Shared.DoAfter;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._CE.GOAP.Actions;

/// <summary>
/// Triggers a synchronous action (Instant, EntityTarget, or WorldTarget).
/// The action type is auto-detected from the components on the action entity;
/// DoAfter-backed actions are rejected because their execution is not synchronous.
/// </summary>
public sealed partial class CEGOAPUseAction : CEGOAPActionBase<CEGOAPUseAction>
{
    /// <summary>
    /// Prototype ID of the action entity to use.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId ActionPrototype;
}

/// <summary>
/// Raised when a target fails validation or its synchronous action event is not handled.
/// Actor-level readiness failures do not reject an otherwise usable target.
/// Carries the target resolved for that exact attempt so optional policies do not have to
/// resolve either the selector or granted action again.
/// </summary>
[ByRefEvent]
public readonly record struct CEGOAPUseActionTargetFailedEvent(
    CEGOAPTargetSelector Selector,
    EntityUid Target);

public sealed partial class CEGOAPUseActionSystem : CEGOAPActionSystem<CEGOAPUseAction>
{
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private IGameTiming _timing = default!;

    [Dependency] private EntityQuery<EntityTargetActionComponent> _entityTargetQuery = default!;
    [Dependency] private EntityQuery<WorldTargetActionComponent> _worldTargetQuery = default!;

    protected override void OnActionInit(
        Entity<CEGOAPComponent> ent,
        ref CEGOAPActionInitEvent<CEGOAPUseAction> args)
    {
        FindOrGrantAction(ent, args.Action.ActionPrototype);
    }

    /// <summary>
    /// During planning: check if the action is on cooldown.
    /// </summary>
    protected override void OnCanExecute(
        Entity<CEGOAPComponent> ent,
        ref CEGOAPActionCanExecuteEvent<CEGOAPUseAction> args)
    {
        var actionEntity = FindActionEntity(ent, args.Action.ActionPrototype);

        if (actionEntity == null ||
            !TryComp<ActionComponent>(actionEntity.Value, out var actionComp) ||
            !actionComp.Enabled ||
            HasComp<DoAfterArgsComponent>(actionEntity.Value))
        {
            args.CanExecute = false;
            return;
        }

        if (_actions.IsCooldownActive(actionComp))
            args.CanExecute = false;
    }

    protected override void OnActionUpdate(
        Entity<CEGOAPComponent> ent,
        ref CEGOAPActionUpdateEvent<CEGOAPUseAction> args)
    {
        if (_timing.ApplyingState)
            return;

        var actionEntity = FindOrGrantAction(ent, args.Action.ActionPrototype);

        if (actionEntity == null)
        {
            args.Status = CEGOAPActionStatus.Failed;
            return;
        }

        if (!TryComp<ActionComponent>(actionEntity.Value, out var actionComp) ||
            HasComp<DoAfterArgsComponent>(actionEntity.Value))
        {
            args.Status = CEGOAPActionStatus.Failed;
            return;
        }

        CEGOAPSelectorResult target = default;
        if (args.Action.Selector != null)
            target = args.Action.Selector.Resolve(ent, EntityManager);

        var action = new Entity<ActionComponent>(actionEntity.Value, actionComp);
        if (!TryValidateAction(ent.Owner, action, target, out var targetFailed))
        {
            args.Status = CEGOAPActionStatus.Failed;
            if (targetFailed)
                RaiseTargetFailed(ent.Owner, args.Action.Selector, target.Entity);
            return;
        }

        // PerformAction raises this instance synchronously and records whether any
        // owning action handler accepted the attempt. Clear it first as PerformAction
        // can return before its own reset when the granted action has stale ownership.
        var actionEvent = _actions.GetEvent(actionEntity.Value);
        if (actionEvent == null)
        {
            args.Status = CEGOAPActionStatus.Failed;
            return;
        }

        actionEvent.Handled = false;
        _actions.PerformAction(
            ent.Owner,
            action,
            actionEvent,
            predicted: false);

        if (actionEvent.Handled)
        {
            args.Status = CEGOAPActionStatus.Finished;
            return;
        }

        args.Status = CEGOAPActionStatus.Failed;
        RaiseTargetFailed(ent.Owner, args.Action.Selector, target.Entity);
    }

    private void RaiseTargetFailed(EntityUid user, CEGOAPTargetSelector? selector, EntityUid? target)
    {
        if (selector != null && target is { } failedTarget)
        {
            var failed = new CEGOAPUseActionTargetFailedEvent(selector, failedTarget);
            RaiseLocalEvent(user, ref failed);
        }
    }

    private bool TryValidateAction(
        EntityUid user,
        Entity<ActionComponent> action,
        CEGOAPSelectorResult target,
        out bool targetFailed)
    {
        targetFailed = false;
        if (!action.Comp.Enabled ||
            action.Comp.AttachedEntity is { } attached && attached != user ||
            _actions.IsCooldownActive(action.Comp))
            return false;

        var hasEntityTarget = _entityTargetQuery.TryComp(action, out var entityTarget);
        var hasWorldTarget = _worldTargetQuery.TryComp(action, out var worldTarget);
        var targetEntity = target.Entity;
        var targetPosition = target.Position;

        if (hasWorldTarget && targetPosition == null && targetEntity is { } positioned &&
            TryComp(positioned, out TransformComponent? transform))
        {
            targetPosition = transform.Coordinates;
        }

        if ((hasEntityTarget && !hasWorldTarget && targetEntity == null) ||
            (hasWorldTarget && targetPosition == null))
            return false;

        RequestPerformActionEvent request;
        var netAction = GetNetEntity(action.Owner);
        if (hasWorldTarget)
        {
            var netCoordinates = GetNetCoordinates(targetPosition!.Value);
            request = hasEntityTarget
                ? new RequestPerformActionEvent(
                    netAction,
                    targetEntity is { } entity ? GetNetEntity(entity) : null,
                    netCoordinates)
                : new RequestPerformActionEvent(netAction, netCoordinates);
        }
        else if (hasEntityTarget)
        {
            request = new RequestPerformActionEvent(netAction, GetNetEntity(targetEntity!.Value));
        }
        else
        {
            request = new RequestPerformActionEvent(netAction);
        }

        var attempt = new ActionAttemptEvent(user);
        RaiseLocalEvent(action.Owner, ref attempt);
        if (attempt.Cancelled || TerminatingOrDeleted(action))
            return false;

        // ValidateEntityTarget also checks the performer. Separate that reason
        // before interpreting its rejection as a failure of the target itself.
        if (action.Comp.CheckConsciousness && !_actionBlocker.CanConsciouslyPerformAction(user) ||
            action.Comp.CheckCanInteract && !_actionBlocker.CanInteract(user, null))
            return false;

        if ((hasWorldTarget &&
             !_actions.ValidateWorldTarget(user, targetPosition!.Value, (action.Owner, worldTarget!))) ||
            (targetEntity is { } entityTargetUid &&
             hasEntityTarget &&
             !_actions.ValidateEntityTarget(user, entityTargetUid, (action.Owner, entityTarget!))))
        {
            targetFailed = true;
            return false;
        }

        var validate = new ActionValidateEvent
        {
            Input = request,
            User = user,
            Provider = action.Comp.Container ?? user,
        };
        RaiseLocalEvent(action.Owner, ref validate);
        return !validate.Invalid && !TerminatingOrDeleted(action) && action.Comp.Running;
    }

    /// <summary>
    /// Finds an already-granted action entity matching the prototype ID.
    /// Does NOT grant a new action; used during planning feasibility checks.
    /// </summary>
    private EntityUid? FindActionEntity(Entity<CEGOAPComponent> ent, EntProtoId actionProto)
    {
        foreach (var action in _actions.GetActions(ent))
        {
            var meta = MetaData(action);
            if (meta.EntityPrototype?.ID == (string) actionProto)
                return action;
        }

        return null;
    }

    /// <summary>
    /// Finds an existing action or grants a new one if not present.
    /// </summary>
    private EntityUid? FindOrGrantAction(Entity<CEGOAPComponent> ent, EntProtoId actionProto)
    {
        var found = FindActionEntity(ent, actionProto);
        if (found != null)
            return found;

        return _actions.AddAction(ent, actionProto);
    }
}
