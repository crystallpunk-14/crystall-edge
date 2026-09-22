using System.Linq;
using Content.Server._CE.Objectives.Target.Components;
using Content.Shared._CE.Objectives;
using Content.Shared._CE.Objectives.Components;
using Content.Shared._CE.Objectives.Target;
using Content.Shared._CE.Objectives.Target.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;

namespace Content.Server._CE.Objectives.Target;

/// <summary>
/// Handles <see cref="CETargetSurviveConditionComponent"/>.
/// </summary>
public sealed partial class CETargetSurviveConditionSystem : EntitySystem
{
    [Dependency] private CETargetObjectiveSystem _target = default!;
    [Dependency] private CESharedObjectiveSystem _objectives = default!;
    [Dependency] private MobStateSystem _mobState = default!;

    [SubscribeLocalEvent]
    private void OnGetProgress(Entity<CETargetSurviveConditionComponent> ent, ref CEGetObjectiveProgressEvent args)
    {
        if (!TryComp<CETargetObjectiveComponent>(ent.Owner, out var targetComp) || targetComp.Target is not { } target)
            return;

        args.Progress = _mobState.IsAlive(target) ? 1f : 0f;
    }

    [SubscribeLocalEvent]
    private void OnTargetChanged(Entity<CETargetSurviveConditionComponent> ent, ref CEObjectiveTargetChangedEvent args)
    {
        if (args.OldTarget is { } oldTarget &&
            !TerminatingOrDeleted(oldTarget) &&
            !_target.GetTargetingObjectives<CETargetSurviveConditionComponent>(oldTarget).Any())
        {
            RemComp<CETargetSurviveConditionMarkerComponent>(oldTarget);
        }

        if (args.NewTarget is { } newTarget)
            EnsureComp<CETargetSurviveConditionMarkerComponent>(newTarget);

        _objectives.RefreshObjectiveProgress(ent.Owner);
    }

    [SubscribeLocalEvent]
    private void OnMobStateChanged(Entity<CETargetSurviveConditionMarkerComponent> ent, ref MobStateChangedEvent args)
    {
        foreach (var objective in _target.GetTargetingObjectives<CETargetSurviveConditionComponent>(ent.Owner))
            _objectives.RefreshObjectiveProgress(objective.Owner);
    }
}
