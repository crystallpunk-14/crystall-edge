namespace Content.Server._CE.Objectives.Components;

/// <summary>
/// Marks an objective whose progress tracks the station's single
/// <see cref="Content.Server._CE.Murk.SphereFixer.CEMurkSphereFixerComponent"/> charge -
/// see <see cref="Content.Server._CE.Objectives.Systems.CEObjectiveLucsonSphereRestoreConditionSystem"/>.
/// </summary>
[RegisterComponent]
public sealed partial class CEObjectiveLucsonSphereRestoreConditionComponent : Component;
