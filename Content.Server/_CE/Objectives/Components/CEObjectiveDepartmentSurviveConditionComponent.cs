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

    /// <summary>
    /// Flips the objective into "every member must be eliminated" - progress becomes the average
    /// of members not surviving, and an empty department counts as fully done.
    /// </summary>
    [DataField]
    public bool Invert;

    /// <summary>
    /// Every body that ever held a role from <see cref="Department"/> this round. Bodies are never
    /// removed - whoever controls a body now, it's that body's survival that counts, and a deleted
    /// body counts as dead.
    /// </summary>
    [DataField]
    public HashSet<EntityUid> Bodies = new();
}
