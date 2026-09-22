using Content.Server._CE.Objectives.Target;

namespace Content.Server._CE.Objectives.Target.Components;

/// <summary>
/// Added to a <see cref="Content.Shared._CE.Objectives.Target.Components.CETargetObjectiveComponent"/>
/// target while it is being targeted by a <see cref="CETargetCompleteOwnedObjectiveComponent"/>.
/// </summary>
[RegisterComponent]
[Access(typeof(CETargetCompleteObjectivesSystem))]
public sealed partial class CETargetCompleteOwnedObjectiveMarkerComponent : Component;
