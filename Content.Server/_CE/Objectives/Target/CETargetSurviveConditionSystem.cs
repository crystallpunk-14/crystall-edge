using Content.Server._CE.Objectives.Target.Components;
using Content.Shared._CE.Objectives.Components;
using Content.Shared._CE.Objectives.Target;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;

namespace Content.Server._CE.Objectives.Target;

/// <summary>
/// Handles <see cref="CETargetSurviveConditionComponent"/>.
/// </summary>
public sealed partial class CETargetSurviveConditionSystem : CEBaseTargetObjectiveSystem<CETargetSurviveConditionComponent>
{
    public override Type[] TargetRelayComponents { get; } = [typeof(CETargetSurviveConditionMarkerComponent)];

    protected override void GetObjectiveProgress(Entity<CETargetSurviveConditionComponent> ent, ref CEGetObjectiveProgressEvent args)
    {
        if (!TargetObjective.TryGetTarget(ent.Owner, out var target))
        {
            args.Progress = ent.Comp.DefaultProgress;
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
}
