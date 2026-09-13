using Content.Shared.Roles.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Roles;

/// <summary>
/// Added to mind role entities to mark them as holding a secret role, and which one.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CESecretRoleComponent : BaseMindRoleComponent
{
    [DataField, AutoNetworkedField]
    public ProtoId<CESecretRolePrototype>? Role;
}
