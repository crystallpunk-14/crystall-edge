using Content.Server._CE.Objectives.Target.Components;
using Content.Shared._CE.Objectives.Components;
using Content.Shared._CE.Objectives.Target;
using Content.Shared.Mobs;

namespace Content.Server._CE.Objectives.Target;

public sealed partial class CETargetSurviveConditionSystem : CEBaseTargetObjectiveSystem<CETargetSurviveConditionComponent>
{
    public override Type[] TargetRelayComponents { get; } = [typeof(CETargetSurviveConditionMarkerComponent)];

    protected override void GetObjectiveProgress(Entity<CETargetSurviveConditionComponent> ent, ref CEGetObjectiveProgressEvent args)
    {
        if (TargetObjective.IsTargetLost(ent.Owner))
        {
            args.Progress = 0f;
            return;
        }

        if (!TargetObjective.TryGetTarget(ent.Owner, out var target))
        {
            args.Progress = ent.Comp.DefaultProgress;
            return;
        }

        if (ObjectivesSys.IsBodyDead(target.Value))
            args.Progress = 0f;
        else if (ObjectivesSys.IsBodyCritical(target.Value))
            args.Progress = 0.5f;
        else
            args.Progress = 1f;
    }

    [SubscribeLocalEvent]
    private void OnMobStateChanged(Entity<CETargetSurviveConditionMarkerComponent> ent, ref MobStateChangedEvent args)
    {
        RefreshTargetingObjectives(ent);
    }
}
