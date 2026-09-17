using Content.Shared._CE.Roles;
using Robust.Shared.Prototypes;

namespace Content.Server._CE.Roles;

/// <summary>
/// Placed on a GameRule entity alongside <see cref="CESecretRoleSelectionComponent"/>.
/// Lets this game mode override the default <see cref="CEObjectivePool"/> a secret role or
/// secret department would otherwise pull from its prototype, without touching the prototype
/// itself (which may be shared by other game modes). See <see cref="CESecretRoleSelectionSystem"/>.
/// </summary>
[RegisterComponent, Access(typeof(CESecretRoleSelectionSystem))]
public sealed partial class CESecretRoleObjectivesOverrideComponent : Component
{
    [DataField]
    public Dictionary<ProtoId<CESecretRolePrototype>, CEObjectivePool> RoleOverrides = new();

    [DataField]
    public Dictionary<ProtoId<CESecretDepartmentPrototype>, CEObjectivePool> DepartmentOverrides = new();
}
