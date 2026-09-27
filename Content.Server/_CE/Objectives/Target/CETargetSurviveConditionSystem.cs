using Content.Server._CE.Objectives.Target.Components;
using Content.Shared._CE.Objectives.Components;
using Content.Shared._CE.Objectives.Target;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;

namespace Content.Server._CE.Objectives.Target;

/// <summary>
/// Handles <see cref="CETargetSurviveConditionComponent"/>.
/// </summary>
public sealed partial class CETargetSurviveConditionSystem : CEBaseTargetObjectiveSystem<CETargetSurviveConditionComponent>
{
    [Dependency] private SharedMindSystem _mind = default!;

    public override Type[] TargetRelayComponents { get; } = [typeof(CETargetSurviveConditionMarkerComponent)];

    protected override void GetObjectiveProgress(Entity<CETargetSurviveConditionComponent> ent, ref CEGetObjectiveProgressEvent args)
    {
        if (!TargetObjective.TryGetTarget(ent.Owner, out var target))
        {
            args.Progress = ent.Comp.DefaultProgress;
            return;
        }

        // No mind left to return to at all - gibbed, converted, wiped, whatever - counts as
        // not having survived, no matter what MobState the leftover body happens to report.
        if (!_mind.TryGetMind(target.Value, out _, out _))
        {
            args.Progress = 0f;
            return;
        }

        if (!TryComp<MobStateComponent>(target.Value, out var mobState))
            return;

        args.Progress = mobState.CurrentState switch
        {
            MobState.Alive => 1f,
            MobState.Critical => 0.5f,
            MobState.Dead => 0f,
            _ => 1f,
        };
    }

    [SubscribeLocalEvent]
    private void OnMobStateChanged(Entity<CETargetSurviveConditionMarkerComponent> ent, ref MobStateChangedEvent args)
    {
        RefreshTargetingObjectives(ent);
    }

    [SubscribeLocalEvent]
    private void OnMindRemoved(Entity<CETargetSurviveConditionMarkerComponent> ent, ref MindRemovedMessage args)
    {
        RefreshTargetingObjectives(ent);
    }
}
