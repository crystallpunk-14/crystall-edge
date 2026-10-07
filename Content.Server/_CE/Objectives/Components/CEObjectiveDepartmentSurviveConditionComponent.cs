using Content.Shared._CE.Roles;
using Robust.Shared.Prototypes;

namespace Content.Server._CE.Objectives.Components;

/// <summary>
/// Marks an objective whose progress is the average survival of every player currently holding a
/// secret role from <see cref="Department"/> - see
/// <see cref="Content.Server._CE.Objectives.Systems.CEObjectiveDepartmentSurviveConditionSystem"/>.
/// </summary>
[RegisterComponent]
public sealed partial class CEObjectiveDepartmentSurviveConditionComponent : Component
{
    [DataField(required: true)]
    public ProtoId<CESecretDepartmentPrototype> Department;
}
