using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared._CE.Roles;
using Content.Shared.Database;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server._CE.Roles;

public sealed partial class CESecretRoleSelectionSystem
{
    /// <summary>
    /// Decides which secret role, if any, each player should receive this round. Touches no
    /// minds/components, so it's directly testable without spinning up a round. Players with a High
    /// role pick first, then Medium, then everyone else - see <see cref="TryPickRole"/> for the draw itself.
    /// </summary>
    public Dictionary<NetUserId, ProtoId<CESecretRolePrototype>> DecideSecretRoles(
        Entity<CESecretRoleSelectionComponent> rule,
        ICommonSession[] players,
        IReadOnlyDictionary<NetUserId, HumanoidCharacterProfile> profiles)
    {
        var available = players
            .Where(session => profiles.ContainsKey(session.UserId) && !HasSecretRole(session))
            .ToList();
        _random.Shuffle(available);

        // Stable sort keeps the shuffled order inside each priority group.
        var ordered = available
            .OrderByDescending(session => GetBestPriority(rule, profiles[session.UserId]))
            .ToList();

        var assignedCounts = new Dictionary<ProtoId<CESecretRolePrototype>, int>(rule.Comp.AssignedCounts);
        var result = new Dictionary<NetUserId, ProtoId<CESecretRolePrototype>>();

        foreach (var session in ordered)
        {
            if (!TryPickRole(rule, assignedCounts, session, profiles[session.UserId], players.Length, out var role))
                continue;

            assignedCounts[role.ID] = assignedCounts.GetValueOrDefault(role.ID) + 1;
            result[session.UserId] = role.ID;
        }

        return result;
    }

    private bool TryAssignLateJoinSecretRole(
        Entity<CESecretRoleSelectionComponent> rule,
        ICommonSession session,
        HumanoidCharacterProfile profile,
        int playerCount)
    {
        if (!TryPickRole(rule, rule.Comp.AssignedCounts, session, profile, playerCount, out var role))
            return false;

        if (GrantSecretRole(rule, session, role) is { } granted)
            GrantSecretRoleObjectives(rule, granted.MindId, granted.Mind, role);

        return true;
    }

    /// <summary>
    /// Draws a department for the player weighted by its free slots times the player's best priority
    /// among its available roles, then a role inside it weighted by role weight times priority. If no
    /// department the player can join has free slots, the draw falls back to share times priority.
    /// </summary>
    private bool TryPickRole(
        Entity<CESecretRoleSelectionComponent> rule,
        IReadOnlyDictionary<ProtoId<CESecretRolePrototype>, int> assignedCounts,
        ICommonSession session,
        HumanoidCharacterProfile profile,
        int playerCount,
        [NotNullWhen(true)] out CESecretRolePrototype? role)
    {
        role = null;

        var options = new List<(CESecretDepartmentSelectorEntry Department, List<(CESecretRoleSelectorEntry Entry, CESecretRolePrototype Role, JobPriority Priority)> Roles, float Multiplier)>();
        foreach (var department in rule.Comp.Departments)
        {
            var roles = GetAvailableRoles(department, assignedCounts, session, profile);
            if (roles.Count == 0)
                continue;

            options.Add((department, roles, GetPriorityMultiplier(roles.Max(r => r.Priority))));
        }

        if (options.Count == 0)
            return false;

        var weights = options
            .Select(o => Math.Max(0, o.Department.GetTargetCount(playerCount) - GetDepartmentCount(o.Department, assignedCounts)) * o.Multiplier)
            .ToList();

        if (weights.Sum() <= 0)
        {
            weights = options.Select(o => o.Department.Share * o.Multiplier).ToList();
            _adminLog.Add(LogType.AntagSelection,
                LogImpact.Low,
                $"{session:player} found no free secret department slot and was drawn over the quotas");
        }

        var picked = options[PickWeightedIndex(weights)];

        // Fallback roles only compete when nothing else is left, or when the player asked for them.
        var candidates = picked.Roles.Where(r => !r.Entry.Fallback || r.Priority > JobPriority.Low).ToList();
        if (candidates.Count == 0)
            candidates = picked.Roles;

        var roleIndex = PickWeightedIndex(candidates.Select(r => r.Entry.Weight * GetPriorityMultiplier(r.Priority)).ToList());
        role = candidates[roleIndex].Role;
        return true;
    }

    /// <summary>
    /// Roles of a department the player could receive right now: eligible and below their cap.
    /// </summary>
    private List<(CESecretRoleSelectorEntry Entry, CESecretRolePrototype Role, JobPriority Priority)> GetAvailableRoles(
        CESecretDepartmentSelectorEntry department,
        IReadOnlyDictionary<ProtoId<CESecretRolePrototype>, int> assignedCounts,
        ICommonSession session,
        HumanoidCharacterProfile profile)
    {
        var result = new List<(CESecretRoleSelectorEntry, CESecretRolePrototype, JobPriority)>();
        foreach (var entry in department.Roles)
        {
            if (!_proto.TryIndex(entry.Role, out var role))
                continue;

            if (entry.Max is { } max && assignedCounts.GetValueOrDefault(entry.Role) >= max)
                continue;

            if (!IsEligible(session, profile, role))
                continue;

            result.Add((entry, role, GetPriority(profile, entry.Role)));
        }

        return result;
    }

    /// <summary>
    /// Players currently in a department. Roles guaranteed by a job sit on top of the quotas, so they
    /// don't take a slot.
    /// </summary>
    private int GetDepartmentCount(
        CESecretDepartmentSelectorEntry department,
        IReadOnlyDictionary<ProtoId<CESecretRolePrototype>, int> assignedCounts)
    {
        _proto.TryIndex(department.Department, out var departmentProto);

        var count = 0;
        foreach (var (roleId, roleCount) in assignedCounts)
        {
            var inDepartment = departmentProto?.Roles.Contains(roleId) == true ||
                               department.Roles.Any(r => r.Role == roleId);
            if (!inDepartment)
                continue;

            if (_proto.TryIndex(roleId, out var role) && role.GuaranteedJobs.Count > 0)
                continue;

            count += roleCount;
        }

        return count;
    }

    private JobPriority GetBestPriority(Entity<CESecretRoleSelectionComponent> rule, HumanoidCharacterProfile profile)
    {
        var best = JobPriority.Low;
        foreach (var department in rule.Comp.Departments)
        {
            foreach (var entry in department.Roles)
            {
                var priority = GetPriority(profile, entry.Role);
                if (priority > best)
                    best = priority;
            }
        }

        return best;
    }

    // "Never" no longer exists in the UI, but old data or an untouched role should still be treated
    // as the Low floor rather than excluded.
    private static JobPriority GetPriority(HumanoidCharacterProfile profile, ProtoId<CESecretRolePrototype> role)
    {
        var priority = profile.SecretRolePriorities.GetValueOrDefault(role, JobPriority.Low);
        return priority < JobPriority.Low ? JobPriority.Low : priority;
    }

    private static float GetPriorityMultiplier(JobPriority priority)
    {
        return priority switch
        {
            JobPriority.High => 10f,
            JobPriority.Medium => 3f,
            _ => 1f,
        };
    }

    /// <summary>
    /// Picks an index with probability proportional to its weight, or uniformly if every weight is zero.
    /// </summary>
    private int PickWeightedIndex(List<float> weights)
    {
        var total = weights.Sum();
        if (total <= 0)
            return _random.Next(weights.Count);

        var roll = _random.NextFloat() * total;
        for (var i = 0; i < weights.Count; i++)
        {
            roll -= weights[i];
            if (roll < 0)
                return i;
        }

        return weights.Count - 1;
    }
}
