using Content.Shared._CE.Roles;
using Robust.Shared.Prototypes;

namespace Content.Server._CE.Objectives.Target.Components;

/// <summary>
/// Rejects target candidates (see <see cref="Content.Shared._CE.Objectives.Target.Components.CETargetObjectiveComponent"/>)
/// whose secret role belongs to one of the listed departments.
/// </summary>
[RegisterComponent]
[Access(typeof(CETargetSecretDepartmentBlacklistSystem))]
public sealed partial class CETargetSecretDepartmentBlacklistComponent : Component
{
    [DataField(required: true)]
    public HashSet<ProtoId<CESecretDepartmentPrototype>> Departments = new();
}
