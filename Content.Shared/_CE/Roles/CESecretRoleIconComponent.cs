using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Roles;

/// <summary>
/// Marks a body as belonging to a secret role and carries which one - granted/removed alongside
/// the role itself by <see cref="Content.Server._CE.Roles.CESecretRoleSelectionSystem"/>. Only
/// networked to viewers <see cref="CESecretRoleIconSystem"/> decides are allowed to recognize this
/// role, so it drives both the status icon above their head and the examine text.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CESecretRoleIconComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public ProtoId<CESecretRolePrototype> Role;

    /// <summary>
    /// Set once the round is over - every viewer but the holder recognizes the role from then on.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Revealed;

    public override bool SessionSpecific => true;
}
