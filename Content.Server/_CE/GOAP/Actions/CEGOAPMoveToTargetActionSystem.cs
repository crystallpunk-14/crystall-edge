using Content.Server._CE.GOAP.Steering;
using Content.Shared._CE.GOAP;
using Content.Shared._CE.GOAP.Components;

namespace Content.Server._CE.GOAP.Actions;

/// <summary>
/// Moves the NPC towards its target slot, across Z-levels if needed.
/// </summary>
public sealed partial class CEGOAPMoveToTargetAction : CEGOAPActionBase<CEGOAPMoveToTargetAction>
{
    /// <summary>
    /// How close the NPC needs to get to the target to consider the action complete.
    /// </summary>
    [DataField]
    public float Range = 1f;
}

public sealed partial class CEGOAPMoveToTargetActionSystem : CEGOAPActionSystem<CEGOAPMoveToTargetAction>
{
    [Dependency] private CEGOAPSteeringSystem _steering = default!;

    protected override void OnActionUpdate(
        Entity<CEGOAPComponent> ent,
        ref CEGOAPActionUpdateEvent<CEGOAPMoveToTargetAction> args)
    {
        if (!TryResolveCoords(ent, args.Action.Target, out var coords))
        {
            args.Status = CEGOAPActionStatus.Failed;
            return;
        }

        args.Status = _steering.Navigate(ent, coords, args.Action.Range) switch
        {
            CEGOAPSteeringStatus.InRange => CEGOAPActionStatus.Finished,
            CEGOAPSteeringStatus.NoPath => CEGOAPActionStatus.Failed,
            _ => CEGOAPActionStatus.Running,
        };
    }

    protected override void OnActionShutdown(
        Entity<CEGOAPComponent> ent,
        ref CEGOAPActionShutdownEvent<CEGOAPMoveToTargetAction> args)
    {
        _steering.Stop(ent.Owner);
    }
}
