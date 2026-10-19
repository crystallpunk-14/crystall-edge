using Content.Server._CE.Objectives.Target.Components;
using Content.Shared._CE.Objectives.Components;
using Content.Shared._CE.Objectives.Target;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;

namespace Content.Server._CE.Objectives.Target;

/// <summary>
/// Handles <see cref="CETargetCritCountConditionComponent"/>.
/// </summary>
public sealed partial class CETargetCritCountConditionSystem : CEBaseTargetObjectiveSystem<CETargetCritCountConditionComponent>
{
    public override Type[] TargetRelayComponents { get; } = [typeof(CETargetCritCountConditionMarkerComponent)];

    protected override void GetObjectiveProgress(Entity<CETargetCritCountConditionComponent> ent, ref CEGetObjectiveProgressEvent args)
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

        // The lesson only counts if they live to learn it.
        if (TryComp<MobStateComponent>(target.Value, out var mobState) && mobState.CurrentState == MobState.Dead)
        {
            args.Progress = 0f;
            return;
        }

        args.Progress = ent.Comp.RequiredCrits <= 0
            ? 1f
            : Math.Min(1f, (float) ent.Comp.Crits / ent.Comp.RequiredCrits);
    }

    [SubscribeLocalEvent]
    private void OnMobStateChanged(Entity<CETargetCritCountConditionMarkerComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.OldMobState == MobState.Alive && args.NewMobState == MobState.Critical)
        {
            foreach (var objective in GetTargetingObjectives(ent))
                objective.Comp.Crits++;
        }

        RefreshTargetingObjectives(ent);
    }
}
