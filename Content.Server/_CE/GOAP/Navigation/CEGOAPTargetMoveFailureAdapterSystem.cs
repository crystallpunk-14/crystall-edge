using Content.Server._CE.GOAP.Actions;
using Content.Shared._CE.GOAP;
using Content.Shared.Interaction;

namespace Content.Server._CE.GOAP.Navigation;

/// <summary>
/// Opt-in contract for selectors whose resolved entity may be temporarily
/// excluded after a terminal movement failure.
/// </summary>
public interface ICEGOAPTargetBackoffSelector
{
}

/// <summary>
/// Adapts terminal results from the existing generic move action into bounded
/// target exclusions. Movement and pathfinding remain owned by the move action.
/// </summary>
public sealed partial class CEGOAPTargetMoveFailureAdapterSystem : EntitySystem
{
    [Dependency] private CEGOAPTargetBackoffSystem _backoff = default!;
    [Dependency] private SharedInteractionSystem _interaction = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CEGOAPTargetBackoffComponent, CEGOAPActionStartupEvent<CEGOAPMoveToTargetAction>>(
            OnMoveStartup,
            after: [typeof(CEGOAPMoveToTargetActionSystem)]);
        SubscribeLocalEvent<CEGOAPTargetBackoffComponent, CEGOAPActionUpdateEvent<CEGOAPMoveToTargetAction>>(
            OnMoveUpdate,
            after: [typeof(CEGOAPMoveToTargetActionSystem)]);
        SubscribeLocalEvent<CEGOAPTargetBackoffComponent, CEGOAPActionShutdownEvent<CEGOAPMoveToTargetAction>>(
            OnMoveShutdown,
            after: [typeof(CEGOAPMoveToTargetActionSystem)]);
    }

    private void OnMoveStartup(
        Entity<CEGOAPTargetBackoffComponent> ent,
        ref CEGOAPActionStartupEvent<CEGOAPMoveToTargetAction> args)
    {
        ent.Comp.ActiveMoveTarget = ResolveTarget(ent.Owner, args.Action);
    }

    private void OnMoveUpdate(
        Entity<CEGOAPTargetBackoffComponent> ent,
        ref CEGOAPActionUpdateEvent<CEGOAPMoveToTargetAction> args)
    {
        if (args.Action.Selector is not ICEGOAPTargetBackoffSelector)
            return;

        var resolved = ResolveTarget(ent.Owner, args.Action);
        if (resolved is not { } target)
        {
            // The generic move action otherwise remains Running when its selector
            // stops resolving a target.
            args.Status = CEGOAPActionStatus.Failed;
            return;
        }

        if (ent.Comp.ActiveMoveTarget is not { } activeTarget || activeTarget != target)
        {
            // The base move action can still expose the previous steering status
            // during a selector retarget. Re-plan cleanly without rejecting the
            // newly resolved target for the old target's result.
            ent.Comp.ActiveMoveTarget = target;
            args.Status = CEGOAPActionStatus.Failed;
            return;
        }

        if (args.Status == CEGOAPActionStatus.Failed)
        {
            _backoff.Reject(ent.Owner, activeTarget);
            return;
        }

        if (args.Status != CEGOAPActionStatus.Finished ||
            _interaction.InRangeAndAccessible(ent.Owner, activeTarget))
            return;

        // Geometric arrival is insufficient for a target behind an obstacle.
        _backoff.Reject(ent.Owner, activeTarget);
        args.Status = CEGOAPActionStatus.Failed;
    }

    private void OnMoveShutdown(
        Entity<CEGOAPTargetBackoffComponent> ent,
        ref CEGOAPActionShutdownEvent<CEGOAPMoveToTargetAction> args)
    {
        ent.Comp.ActiveMoveTarget = null;
    }

    private EntityUid? ResolveTarget(EntityUid agent, CEGOAPMoveToTargetAction action)
    {
        if (action.Selector is not ICEGOAPTargetBackoffSelector)
            return null;

        return action.Selector.Resolve(agent, EntityManager).Entity;
    }
}
