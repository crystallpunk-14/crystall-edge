using Content.Server._CE.Objectives.Target.Components;
using Content.Shared._CE.Objectives.Components;
using Content.Shared._CE.Objectives.Target;
using Content.Shared.Mobs;

namespace Content.Server._CE.Objectives.Target;

public sealed partial class CETargetKillConditionSystem : CEBaseTargetObjectiveSystem<CETargetKillConditionComponent>
{
    public override Type[] TargetRelayComponents { get; } = [typeof(CETargetKillConditionMarkerComponent)];

    protected override void GetObjectiveProgress(Entity<CETargetKillConditionComponent> ent, ref CEGetObjectiveProgressEvent args)
    {
        if (TargetObjective.IsTargetLost(ent.Owner))
        {
            args.Progress = 1f;
            return;
        }

        if (!TargetObjective.TryGetTarget(ent.Owner, out var target))
        {
            args.Progress = ent.Comp.DefaultProgress;
            return;
        }

        args.Progress = ObjectivesSys.GetEliminationProgress(target.Value);
    }

    [SubscribeLocalEvent]
    private void OnMobStateChanged(Entity<CETargetKillConditionMarkerComponent> ent, ref MobStateChangedEvent args)
    {
        RefreshTargetingObjectives(ent);
    }
}
