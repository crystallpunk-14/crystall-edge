using Content.Server._CE.Objectives.Target;
using Content.Shared._CE.Roles;
using Robust.Shared.Prototypes;

namespace Content.Server._CE.Objectives.Target.Components;

/// <summary>
/// Rejects target candidates (see <see cref="Content.Shared._CE.Objectives.Target.Components.CETargetObjectiveComponent"/>)
/// who hold one of the listed secret roles - e.g. so a Tormentor can't target a fellow Tormentor.
/// </summary>
[RegisterComponent]
[Access(typeof(CETargetSecretRoleBlacklistSystem))]
public sealed partial class CETargetSecretRoleBlacklistComponent : Component
{
    [DataField(required: true)]
    public HashSet<ProtoId<CESecretRolePrototype>> SecretRoleBlacklist = new();
}
