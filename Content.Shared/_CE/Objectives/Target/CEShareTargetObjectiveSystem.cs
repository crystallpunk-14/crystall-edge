using Content.Shared._CE.Objectives.Components;
using Content.Shared._CE.Objectives.Target.Components;
using Content.Shared.Whitelist;

namespace Content.Shared._CE.Objectives.Target;

/// <summary>
/// Handles <see cref="CEShareTargetObjectiveComponent"/>.
/// </summary>
public sealed partial class CEShareTargetObjectiveSystem : CEBaseObjectiveSystem<CEShareTargetObjectiveComponent>
{
    [Dependency] private EntityWhitelistSystem _entityWhitelist = default!;
    [Dependency] private CETargetObjectiveSystem _targetObjective = default!;

    protected override void InitializeObjective(Entity<CEShareTargetObjectiveComponent> ent, ref CEInitializeObjectiveEvent args)
    {
        TryResolveTarget(ent, args.Holder.AsNullable());
    }

    // A holder's objectives (e.g. from a weighted CEObjectivePool) aren't created in a guaranteed
    // order, so the objective we're meant to share a target from might not exist yet when our own
    // CEInitializeObjectiveEvent fires. Retry on every subsequent change to the holder's objective
    // list - each objective's own creation triggers one of these, including whichever one adds the
    // source objective we're waiting on.
    [SubscribeLocalEvent]
    private void OnObjectivesChanged(Entity<CEObjectiveHolderComponent> ent, ref CEObjectivesChangedEvent args)
    {
        foreach (var objective in ObjectivesSys.GetObjectives(ent.AsNullable()))
        {
            if (!TryComp<CEShareTargetObjectiveComponent>(objective.Owner, out var share))
                continue;

            if (TryComp<CETargetObjectiveComponent>(objective.Owner, out var targetComp) && targetComp.Target != null)
                continue; // already resolved

            TryResolveTarget((objective.Owner, share), ent.AsNullable());
        }
    }

    private void TryResolveTarget(Entity<CEShareTargetObjectiveComponent> ent, Entity<CEObjectiveHolderComponent?> holder)
    {
        foreach (var objective in ObjectivesSys.GetObjectives(holder))
        {
            if (objective.Owner == ent.Owner)
                continue;

            if (!TryComp<CETargetObjectiveComponent>(objective.Owner, out var targetComp) || targetComp.Target == null)
                continue;

            if (_entityWhitelist.IsWhitelistFail(ent.Comp.ObjectiveWhitelist, objective.Owner))
                continue;

            _targetObjective.SetTarget(ent.Owner, targetComp.Target);
            break;
        }
    }
}
