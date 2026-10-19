using Content.Shared._CE.Roles;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Recruitment;

/// <summary>
/// An action that invites the target player to join <see cref="Role"/>'s department as that role.
/// They get a dialog to accept or decline; the recruiter is told the answer in chat. Invalid
/// targets cancel the action before it's used, so no cooldown is spent on them.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class CERecruitmentActionComponent : Component
{
    [DataField(required: true)]
    public ProtoId<CESecretRolePrototype> Role;

    /// <summary>
    /// How long the target has to answer before it counts as a refusal.
    /// </summary>
    [DataField]
    public TimeSpan Timeout = TimeSpan.FromSeconds(60);
}
