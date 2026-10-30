using Content.Server._CE.Objectives.Target;

namespace Content.Server._CE.Objectives.Target.Components;

/// <summary>
/// Rejects target candidates (see <see cref="Content.Shared._CE.Objectives.Target.Components.CETargetObjectiveComponent"/>)
/// already targeted by another objective of the holder's <c>CESecretDepartmentPrototype</c> - shared
/// department objectives and every member's personal ones alike. Put it on every targeted objective
/// of the department so they stay apart regardless of the order their targets get picked in.
/// </summary>
[RegisterComponent]
[Access(typeof(CETargetExcludeDepartmentTargetsSystem))]
public sealed partial class CETargetExcludeDepartmentTargetsComponent : Component;
