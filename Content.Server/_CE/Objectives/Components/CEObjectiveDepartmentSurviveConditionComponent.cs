using Content.Shared._CE.Roles;
using Robust.Shared.Prototypes;

namespace Content.Server._CE.Objectives.Components;

/// <summary>
/// Marks an objective whose progress is the average survival of every player currently holding a
/// secret role from <see cref="Department"/> or <see cref="Roles"/> - see
/// <see cref="Content.Server._CE.Objectives.Systems.CEObjectiveDepartmentSurviveConditionSystem"/>.
/// </summary>
[RegisterComponent]
public sealed partial class CEObjectiveDepartmentSurviveConditionComponent : Component
{
    [DataField]
    public ProtoId<CESecretDepartmentPrototype>? Department;

    /// <summary>
    /// Individual secret roles counted as members on top of <see cref="Department"/>.
    /// </summary>
    [DataField]
    public HashSet<ProtoId<CESecretRolePrototype>> Roles = new();

    /// <summary>
    /// Flips the objective into "every member must be eliminated" - progress becomes the average
    /// of members not surviving, and an empty department counts as fully done.
    /// </summary>
    [DataField]
    public bool Invert;
}
