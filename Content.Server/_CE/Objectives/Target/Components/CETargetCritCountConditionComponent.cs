using Content.Server._CE.Objectives.Target;

namespace Content.Server._CE.Objectives.Target.Components;

/// <summary>
/// Objective condition that succeeds once the objective's target (see
/// <see cref="Content.Shared._CE.Objectives.Target.Components.CETargetObjectiveComponent"/>) went
/// into critical condition <see cref="RequiredCrits"/> times, while still being alive.
/// </summary>
[RegisterComponent]
[Access(typeof(CETargetCritCountConditionSystem))]
public sealed partial class CETargetCritCountConditionComponent : Component
{
    /// <summary>
    /// How many times the target has to go from alive into critical condition.
    /// </summary>
    [DataField]
    public int RequiredCrits = 3;

    /// <summary>
    /// How many times the target went into critical condition so far. Lives on the objective, so
    /// it carries over when the target's mind moves into another body.
    /// </summary>
    [DataField]
    public int Crits;

    /// <summary>
    /// Progress shown while no target has been picked yet.
    /// </summary>
    [DataField]
    public float DefaultProgress;
}
