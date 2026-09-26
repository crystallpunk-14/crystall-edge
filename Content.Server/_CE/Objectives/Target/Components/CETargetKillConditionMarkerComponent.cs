using Content.Server._CE.Objectives.Target;

namespace Content.Server._CE.Objectives.Target.Components;

/// <summary>
/// Added to a target while it is being targeted by a <see cref="CETargetKillConditionComponent"/>,
/// so mob state changes on it can refresh that objective's progress.
/// </summary>
[RegisterComponent]
[Access(typeof(CETargetKillConditionSystem))]
public sealed partial class CETargetKillConditionMarkerComponent : Component;
