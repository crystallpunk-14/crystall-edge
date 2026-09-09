using Content.Shared._CE.GOAP;
using Content.Shared._CE.GOAP.Components;
using Content.Shared._CE.GOAP.Selectors;
using Content.Shared._CE.Actions;
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

        CEGOAPSelectorResult target = default;
        if (args.Action.Selector != null)
            target = args.Action.Selector.Resolve(ent, EntityManager);

        args.Target = target.Entity;
        if (!TryCreateRequest(actionEntity.Value, target, out var request))
        {
            args.Status = CEGOAPActionStatus.Failed;
            return;
        }

        var result = _actions.TryPerformActionChecked(request, ent.Owner, allowDoAfter: false, predicted: false, showPopups: false);
        args.Status = result == CEActionExecutionResult.Performed ? CEGOAPActionStatus.Finished : CEGOAPActionStatus.Failed;
        // A cooldown, action blocker or exhausted charge must not blacklist a usable destination.
        if (result is CEActionExecutionResult.InvalidTarget or CEActionExecutionResult.Unhandled)
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

    private bool TryCreateRequest(
        EntityUid action,
        CEGOAPSelectorResult target,
        out RequestPerformActionEvent request)
    {
        request = default!;
        var hasEntityTarget = _entityTargetQuery.HasComp(action);
        var hasWorldTarget = _worldTargetQuery.HasComp(action);
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

        var netAction = GetNetEntity(action);
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

        return true;
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
