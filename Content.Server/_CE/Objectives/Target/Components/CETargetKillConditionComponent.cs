using Content.Server._CE.Objectives.Target;

namespace Content.Server._CE.Objectives.Target.Components;

/// <summary>
/// Objective condition that succeeds once the objective's target (see
/// <see cref="Content.Shared._CE.Objectives.Target.Components.CETargetObjectiveComponent"/>) dies,
/// and fails while they're still alive. Inverse of <see cref="CETargetSurviveConditionComponent"/>.
/// </summary>
[RegisterComponent]
[Access(typeof(CETargetKillConditionSystem))]
public sealed partial class CETargetKillConditionComponent : Component
{
    /// <summary>
    /// Progress shown while no target has been picked yet.
    /// </summary>
    [DataField]
    public float DefaultProgress;
}
