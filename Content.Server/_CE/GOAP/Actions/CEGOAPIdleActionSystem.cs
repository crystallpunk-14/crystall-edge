using Content.Server._CE.GOAP.Steering;
using Content.Shared._CE.GOAP;
using Content.Shared._CE.GOAP.Components;

namespace Content.Server._CE.GOAP.Actions;

/// <summary>
/// Stands still until the planner switches to something else.
/// </summary>
public sealed partial class CEGOAPIdleAction : CEGOAPActionBase<CEGOAPIdleAction>;

public sealed partial class CEGOAPIdleActionSystem : CEGOAPActionSystem<CEGOAPIdleAction>
{
    [Dependency] private CEGOAPSteeringSystem _steering = default!;

    protected override void OnActionStartup(
        Entity<CEGOAPComponent> ent,
        ref CEGOAPActionStartupEvent<CEGOAPIdleAction> args)
    {
        _steering.Stop(ent.Owner);
    }

    protected override void OnActionUpdate(
        Entity<CEGOAPComponent> ent,
        ref CEGOAPActionUpdateEvent<CEGOAPIdleAction> args)
    {
        args.Status = CEGOAPActionStatus.Running;
    }
}
