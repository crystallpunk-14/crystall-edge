using Content.Shared._CE.Objectives.Components;
using Content.Shared._CE.Objectives.Target;
using Content.Shared._CE.Objectives.Target.Components;
using Content.Shared._CE.Waypointer;
using Content.Shared.Mind;
using Content.Shared.StatusEffectNew;
using Content.Shared.StatusEffectNew.Components;

namespace Content.Shared._CE.ObjectiveWaypointer;

/// <summary>
/// Glue between objectives and waypointers: feeds the targets of an entity's objectives into its
/// waypointers while it has a <see cref="CEObjectiveTargetsWaypointerStatusEffectComponent"/> status,
/// and refreshes them whenever those targets change.
/// </summary>
public sealed partial class CEObjectiveWaypointerSystem : EntitySystem
{
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private CETargetObjectiveSystem _targetObjective = default!;
    [Dependency] private CESharedWaypointerSystem _waypointer = default!;

    [SubscribeLocalEvent]
    private void OnStatusApplied(Entity<CEObjectiveTargetsWaypointerStatusEffectComponent> ent, ref StatusEffectAppliedEvent args)
    {
        _waypointer.RefreshWaypointers(args.Target);
    }

    [SubscribeLocalEvent]
    private void OnStatusRemoved(Entity<CEObjectiveTargetsWaypointerStatusEffectComponent> ent, ref StatusEffectRemovedEvent args)
    {
        _waypointer.RefreshWaypointers(args.Target);
    }

    [SubscribeLocalEvent]
    private void OnRefreshWaypointers(Entity<CEObjectiveTargetsWaypointerStatusEffectComponent> ent, ref StatusEffectRelayedEvent<CERefreshWaypointersEvent> args)
    {
        if (!_mind.TryGetMind(args.AppliedTo, out var mindId, out _) || !TryComp<CEObjectiveHolderComponent>(mindId, out var holder))
            return;

        foreach (var objective in holder.Objectives)
        {
            if (TryComp<CETargetObjectiveComponent>(objective, out var targetComp) &&
                _targetObjective.TryGetTarget((objective, targetComp), out var target) &&
                target != args.AppliedTo)
            {
                args.Args.TrackedEntities[target.Value] = ent.Comp.Waypointer;
            }
        }
    }

    [SubscribeLocalEvent]
    private void OnObjectivesChanged(ref CEObjectivesChangedEvent args)
    {
        RefreshAll();
    }

    [SubscribeLocalEvent]
    private void OnObjectiveTargetChanged(ref CEObjectiveTargetChangedEvent args)
    {
        RefreshAll();
    }

    private void RefreshAll()
    {
        var query = EntityQueryEnumerator<CEObjectiveTargetsWaypointerStatusEffectComponent, StatusEffectComponent>();
        while (query.MoveNext(out _, out _, out var status))
        {
            if (status.AppliedTo is { } appliedTo)
                _waypointer.RefreshWaypointers(appliedTo);
        }
    }
}
