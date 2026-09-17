using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server._CE.GameTicking;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Server.Mind;
using Content.Server.Players.PlayTimeTracking;
using Content.Server.Roles;
using Content.Shared._CE.Murk.Components;
using Content.Shared._CE.Objectives.Components;
using Content.Shared._CE.Roles;
using Content.Shared._CE.Roundflow;
using Content.Shared.GameTicking;
using Content.Shared.Ghost.Components;
using Content.Shared.Mind;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Content.Shared.Roles.Components;
using Robust.Server.Player;
using Robust.Shared.Audio;
using Robust.Shared.Enums;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._CE.Roles;

public sealed partial class CESecretRoleSelectionSystem : GameRuleSystem<CESecretRoleSelectionComponent>
{
    private static readonly EntProtoId MindRoleSecret = "CEMindRoleSecret";

    [Dependency] private IPlayerManager _playerManager = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private MindSystem _mind = default!;
    [Dependency] private RoleSystem _role = default!;
    [Dependency] private PlayTimeTrackingManager _playTimeTracking = default!;

    /// <summary>
    /// The Lucson Sphere just cracked - reveal every player's already-assigned secret role and its goal.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnRoundStart(CERoundStartEvent ev)
    {
        foreach (var session in _playerManager.Sessions)
        {
            SendRolePopup(session);
        }
    }

    [SubscribeLocalEvent]
    private void OnJobsAssigned(RulePlayerJobsAssignedEvent args)
    {
        var query = QueryActiveRules();
        while (query.MoveNext(out var uid, out _, out var comp, out _))
        {
            AssignSecretRoles((uid, comp), args.Players, args.Profiles);
        }
    }

    [SubscribeLocalEvent(after: [typeof(CEMurkConsumingRuleSystem)])]
    private void OnPlayerSpawnComplete(PlayerSpawnCompleteEvent args)
    {
        if (!args.LateJoin)
            return;

        if (HasSecretRole(args.Player))
            return;

        var playerCount = GetActivePlayerCount();

        var activeRules = QueryActiveRules();
        while (activeRules.MoveNext(out var uid, out _, out var roleSelection, out _))
        {
            if (!TryAssignLateJoinSecretRole((uid, roleSelection), args.Player, args.Profile, playerCount))
                continue;

            var sphereQuery = EntityQueryEnumerator<CEMurkLusconSphereComponent>();
            while (sphereQuery.MoveNext(out _, out var sphere))
            {
                if (sphere.State == CEMurkSphereState.Cracked)
                {
                    SendRolePopup(args.Player);
                    break;
                }
            }
            return;
        }
    }

    /// <summary>
    /// Clears a player's current secret role (if any) - including its objectives - and grants
    /// them a new one via whichever active rule is running (used as the bookkeeping container for
    /// AssignedCounts/DepartmentObjectives even if it doesn't list this role in its own Roles).
    /// Reused by admin tooling (<c>secretroleset</c>); round-start/late-join assignment calls
    /// <see cref="GrantSecretRole"/> directly since there's nothing to clear yet.
    /// </summary>
    public bool TrySetSecretRole(ICommonSession session,
        ProtoId<CESecretRolePrototype> roleId,
        [NotNullWhen(false)] out string? error,
        bool removeSkills = false)
    {
        error = null;

        if (!_proto.TryIndex(roleId, out var role))
        {
            error = $"Unknown secret role '{roleId}'";
            return false;
        }

        Entity<CESecretRoleSelectionComponent>? targetRule = null;
        var rules = QueryActiveRules();
        if (rules.MoveNext(out var uid, out _, out var comp, out _))
            targetRule = (uid, comp);

        if (targetRule is null)
        {
            error = "No active secret role rule is running";
            return false;
        }

        if (!_mind.TryGetMind(session, out var mindId, out var mind))
        {
            error = "Player has no mind";
            return false;
        }

        ClearSecretRole(mindId, mind, removeSkills);
        GrantSecretRole(targetRule.Value, session, role);

        var sphereQuery = EntityQueryEnumerator<CEMurkLusconSphereComponent>();
        while (sphereQuery.MoveNext(out _, out var sphere))
        {
            if (sphere.State == CEMurkSphereState.Cracked)
            {
                SendRolePopup(session);
                break;
            }
        }

        return true;
    }

    /// <summary>
    /// Removes a mind's current secret role and every objective it was holding, and rolls back
    /// the granting rule's assigned-count bookkeeping so future assignment stays consistent.
    /// Skills granted by the role/department are only stripped if <paramref name="removeSkills"/>
    /// is set - by default a role swap leaves previously-learned skills in place.
    /// </summary>
    private void ClearSecretRole(EntityUid mindId, MindComponent mind, bool removeSkills = false)
    {
        if (!_role.MindHasRole<CESecretRoleComponent>((mindId, mind), out var roleEnt))
            return;

        if (roleEnt.Value.Comp2.Role is { } oldRoleId)
        {
            var rules = QueryActiveRules();
            while (rules.MoveNext(out _, out _, out var comp, out _))
            {
                if (comp.AssignedCounts.TryGetValue(oldRoleId, out var count) && count > 0)
                    comp.AssignedCounts[oldRoleId] = count - 1;
            }

            if (removeSkills && mind.OwnedEntity is { } target)
                RemoveSecretRoleSkills(target, oldRoleId);
        }

        if (TryComp<CEObjectiveHolderComponent>(mindId, out var holderComp))
        {
            foreach (var objective in holderComp.Objectives.ToList())
                _ceObjectives.TryRemoveObjective(mindId, objective);
        }

        _role.MindRemoveRole<CESecretRoleComponent>((mindId, mind));
    }

    private void SendRolePopup(ICommonSession session)
    {
        if (!_mind.TryGetMind(session, out var mindId, out var mind)
            || !_role.MindHasRole<CESecretRoleComponent>((mindId, mind), out var roleEnt))
            return;

        if (roleEnt.Value.Comp2.Role is not { } roleId || !_proto.TryIndex(roleId, out var role))
            return;

        var goalText = TryComp<RoleBriefingComponent>(roleEnt.Value.Owner, out var briefing)
            ? Loc.GetString(briefing.Briefing)
            : string.Empty;

        RaiseNetworkEvent(new CEScreenPopupShowEvent(role.LocalizedName, goalText,
            new SoundPathSpecifier("/Audio/_CE/Announce/darkness_boom.ogg")), session);
    }

    private void AssignSecretRoles(
        Entity<CESecretRoleSelectionComponent> rule,
        ICommonSession[] players,
        IReadOnlyDictionary<NetUserId, HumanoidCharacterProfile> profiles)
    {
        // Players still eligible to receive a secret role this round - at most one each,
        // and never someone who already has a secret role from another active rule.
        var available = players
            .Where(session => profiles.ContainsKey(session.UserId) && !HasSecretRole(session))
            .ToHashSet();

        var entries = rule.Comp.Roles.OrderByDescending(entry => entry.Weight);

        foreach (var entry in entries)
        {
            if (!_proto.TryIndex(entry.Role, out var role))
                continue;

            var target = entry.GetTargetCount(players.Length);
            while (GetAssignedCount(rule, entry.Role) < target)
            {
                if (!TryPickCandidate(role, available, profiles, out var picked))
                    break;

                available.Remove(picked);
                GrantSecretRole(rule, picked, role);
            }
        }
    }

    private bool TryAssignLateJoinSecretRole(
        Entity<CESecretRoleSelectionComponent> rule,
        ICommonSession session,
        HumanoidCharacterProfile profile,
        int playerCount)
    {
        foreach (var entry in rule.Comp.Roles.OrderByDescending(e => e.Weight))
        {
            if (!_proto.TryIndex(entry.Role, out var role))
                continue;

            if (GetAssignedCount(rule, entry.Role) >= entry.GetTargetCount(playerCount))
                continue;

            if (!IsEligible(session, profile, role))
                continue;

            GrantSecretRole(rule, session, role);
            return true;
        }

        return false;
    }

    private bool TryPickCandidate(
        CESecretRolePrototype role,
        HashSet<ICommonSession> available,
        IReadOnlyDictionary<NetUserId, HumanoidCharacterProfile> profiles,
        out ICommonSession picked)
    {
        for (var tier = JobPriority.High; tier >= JobPriority.Low; tier--)
        {
            var candidates = new List<ICommonSession>();

            foreach (var session in available)
            {
                var profile = profiles[session.UserId];

                // "Never" no longer exists in the UI, but old data or an untouched role should
                // still be treated as the Low floor rather than excluded.
                var priority = profile.SecretRolePriorities.GetValueOrDefault(role.ID, JobPriority.Low);
                if (priority < JobPriority.Low)
                    priority = JobPriority.Low;

                if (priority != tier)
                    continue;

                if (!IsEligible(session, profile, role))
                    continue;

                candidates.Add(session);
            }

            if (candidates.Count == 0)
                continue;

            picked = _random.Pick(candidates);
            return true;
        }

        picked = default!;
        return false;
    }

    private bool IsEligible(ICommonSession session, HumanoidCharacterProfile profile, CESecretRolePrototype role)
    {
        _playTimeTracking.TryGetTrackerTimes(session, out var playTimes);
        return JobRequirements.TryRequirementsMet(
            role.Requirements,
            playTimes ?? new Dictionary<string, TimeSpan>(),
            out _,
            EntityManager,
            _proto,
            profile,
            session.UserId);
    }

    private bool HasSecretRole(ICommonSession session)
    {
        return _mind.TryGetMind(session, out var mindId, out _) && _role.MindHasRole<CESecretRoleComponent>(mindId);
    }

    private int GetAssignedCount(Entity<CESecretRoleSelectionComponent> rule, ProtoId<CESecretRolePrototype> role)
    {
        return rule.Comp.AssignedCounts.GetValueOrDefault(role);
    }

    private void GrantSecretRole(Entity<CESecretRoleSelectionComponent> rule, ICommonSession session, CESecretRolePrototype role)
    {
        if (!_mind.TryGetMind(session, out var mindId, out var mind))
            return;

        _role.MindAddRole(mindId, MindRoleSecret, mind, silent: true);

        if (!_role.MindHasRole<CESecretRoleComponent>((mindId, mind), out var roleEnt))
            return;

        roleEnt.Value.Comp2.Role = role.ID;
        rule.Comp.AssignedCounts[role.ID] = GetAssignedCount(rule, role.ID) + 1;

        if (role.Briefing is { } briefing)
            EnsureComp<RoleBriefingComponent>(roleEnt.Value.Owner).Briefing = briefing;

        GrantSecretRoleObjectives(rule, mindId, mind, role);

        if (session.AttachedEntity is { } target)
            GrantSecretRoleSkills(target, role);
    }

    /// <summary>
    /// Mirrors AntagSelectionSystem.GetActivePlayerCount: connected sessions with a live, non-ghost body.
    /// </summary>
    private int GetActivePlayerCount()
    {
        var count = 0;
        foreach (var session in _playerManager.Sessions)
        {
            if (session.Status is SessionStatus.Disconnected or SessionStatus.Zombie)
                continue;

            if (session.AttachedEntity is not { } uid || HasComp<GhostComponent>(uid))
                continue;

            count++;
        }

        return count;
    }
}
