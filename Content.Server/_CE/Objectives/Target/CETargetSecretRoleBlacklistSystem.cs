using Content.Server._CE.Objectives.Target.Components;
using Content.Server.Roles;
using Content.Shared._CE.Objectives.Target.Components;
using Content.Shared._CE.Roles;
using Content.Shared.Mind;

namespace Content.Server._CE.Objectives.Target;

/// <summary>
/// Handles <see cref="CETargetSecretRoleBlacklistComponent"/>.
/// </summary>
public sealed partial class CETargetSecretRoleBlacklistSystem : EntitySystem
{
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private RoleSystem _role = default!;

    [SubscribeLocalEvent]
    private void OnValidateCandidate(Entity<CETargetSecretRoleBlacklistComponent> ent, ref CEValidateObjectiveTargetCandidateEvent args)
    {
        if (!_mind.TryGetMind(args.Candidate, out var candidateMindId, out _))
            return;

        if (_role.MindHasRole<CESecretRoleComponent>(candidateMindId, out var roleEnt) &&
            roleEnt.Value.Comp2.Role is { } roleId &&
            ent.Comp.SecretRoleBlacklist.Contains(roleId))
        {
            args.Invalidate();
        }
    }
}
