using Content.Server._CE.Objectives.Target;

namespace Content.Server._CE.Objectives.Target.Components;

/// <summary>
/// Objective condition that succeeds while the objective's target (see
/// <see cref="Content.Shared._CE.Objectives.Target.Components.CETargetObjectiveComponent"/>) is alive,
/// and fails once they die.
/// </summary>
[RegisterComponent]
[Access(typeof(CETargetSurviveConditionSystem))]
public sealed partial class CETargetSurviveConditionComponent : Component;
