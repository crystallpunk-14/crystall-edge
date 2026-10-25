using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Roles;

/// <summary>
/// The original-role counterpart of <see cref="CESecretRoleIconComponent"/> - only present while the
/// body's original secret role differs from the current one. Networked separately, so a viewer only
/// learns the original role if <see cref="CESecretRoleIconSystem"/> lets them recognize it.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CESecretRoleOriginalIconComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public ProtoId<CESecretRolePrototype> Role;

    /// <inheritdoc cref="CESecretRoleIconComponent.Revealed"/>
    [DataField, AutoNetworkedField]
    public bool Revealed;

    public override bool SessionSpecific => true;
}
