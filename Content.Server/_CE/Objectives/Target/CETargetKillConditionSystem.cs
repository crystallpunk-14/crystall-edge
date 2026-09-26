using Content.Server._CE.Objectives.Target.Components;
using Content.Shared._CE.Objectives.Components;
using Content.Shared._CE.Objectives.Target;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;

namespace Content.Server._CE.Objectives.Target;

/// <summary>
/// Handles <see cref="CETargetKillConditionComponent"/>.
/// </summary>
public sealed partial class CETargetKillConditionSystem : CEBaseTargetObjectiveSystem<CETargetKillConditionComponent>
{
    public override Type[] TargetRelayComponents { get; } = [typeof(CETargetKillConditionMarkerComponent)];

    protected override void GetObjectiveProgress(Entity<CETargetKillConditionComponent> ent, ref CEGetObjectiveProgressEvent args)
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
            MobState.Dead => 1f,
            MobState.Critical => 0.5f,
            MobState.Alive => 0f,
            _ => 0f,
        };
    }

    [SubscribeLocalEvent]
    private void OnMobStateChanged(Entity<CETargetKillConditionMarkerComponent> ent, ref MobStateChangedEvent args)
    {
        RefreshTargetingObjectives(ent);
    }
}
