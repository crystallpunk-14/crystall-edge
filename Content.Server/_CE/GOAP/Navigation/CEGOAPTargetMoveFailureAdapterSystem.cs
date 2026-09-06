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
        SubscribeLocalEvent<CEGOAPTargetBackoffComponent, CEGOAPActionUpdateEvent<CEGOAPMoveToTargetAction>>(
            OnMoveUpdate,
            after: [typeof(CEGOAPMoveToTargetActionSystem)]);
    }

    private void OnMoveUpdate(
        Entity<CEGOAPTargetBackoffComponent> ent,
        ref CEGOAPActionUpdateEvent<CEGOAPMoveToTargetAction> args)
    {
        if (args.Action.Selector is not ICEGOAPTargetBackoffSelector || args.Target is not { } target)
            return;

        if (args.Status == CEGOAPActionStatus.Failed)
        {
            _backoff.Reject(ent.Owner, target);
            return;
        }

        if (args.Status != CEGOAPActionStatus.Finished ||
            _interaction.InRangeAndAccessible(ent.Owner, target))
            return;

        // Geometric arrival is insufficient for a target behind an obstacle.
        _backoff.Reject(ent.Owner, target);
        args.Status = CEGOAPActionStatus.Failed;
    }
}
