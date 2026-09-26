namespace Content.Server._CE.Objectives.Components;

/// <summary>
/// Marks an objective whose progress tracks how close the station's Lucson Sphere is to
/// collapsing into the murk - see
/// <see cref="Content.Server._CE.Objectives.Systems.CEObjectiveMurkSphereCollapseConditionSystem"/>.
/// </summary>
[RegisterComponent]
public sealed partial class CEObjectiveMurkSphereCollapseConditionComponent : Component;
