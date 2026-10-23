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

    /// <summary>
    /// The first secret role this mind was granted. Kept through recruitment, so objectives aimed at
    /// the original role still count this mind and its old department still recognizes it.
    /// Equal to <see cref="Role"/> while the role has never changed.
    /// </summary>
    [DataField, AutoNetworkedField]
    public ProtoId<CESecretRolePrototype>? OriginalRole;
}
