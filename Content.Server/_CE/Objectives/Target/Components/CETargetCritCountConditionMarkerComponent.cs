using Content.Server._CE.Objectives.Target;

namespace Content.Server._CE.Objectives.Target.Components;

/// <summary>
/// Added to a target while it is being targeted by a <see cref="CETargetCritCountConditionComponent"/>,
/// so mob state changes on it can count crits and refresh that objective's progress.
/// </summary>
[RegisterComponent]
[Access(typeof(CETargetCritCountConditionSystem))]
public sealed partial class CETargetCritCountConditionMarkerComponent : Component;
