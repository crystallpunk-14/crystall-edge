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
    /// AssignedCounts/DepartmentObjectiveHolders even if it doesn't list this role in its own Roles).
    /// Reused by admin tooling (<c>secretroleset</c>); round-start/late-join assignment calls
    /// <see cref="GrantSecretRole"/> directly since there's nothing to clear yet.
    /// </summary>
    public bool TrySetSecretRole(ICommonSession session,
        ProtoId<CESecretRolePrototype> roleId,
        [NotNullWhen(false)] out string? error,
        bool removeSkills = true)
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
        if (GrantSecretRole(targetRule.Value, session, role) is { } granted)
            GrantSecretRoleObjectives(targetRule.Value, granted.MindId, granted.Mind, role);

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

    private void ClearSecretRole(EntityUid mindId, MindComponent mind, bool removeSkills = true)
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

        // Only ever removes objectives this mind actually owns (its personal role-pool draw) -
        // the department's shared ones belong to the department's own holder entity and just drop
        // off this mind's list on the RegenerateObjectiveList call below.
        if (TryComp<CEObjectiveHolderComponent>(mindId, out var holderComp))
        {
            foreach (var objective in holderComp.OwnedObjectives.ToList())
                _objectives.TryRemoveObjective(mindId, objective);
        }

        _role.MindRemoveRole<CESecretRoleComponent>((mindId, mind));

        // Membership just changed - re-query CEGetAdditionalObjectivesEvent so a department's
        // shared objectives (if any) disappear from this mind's list and PVS overrides.
        _objectives.RegenerateObjectiveList(mindId);
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

        // Grant all roles first, create objectives after - a target-based objective (e.g. Lover)
        // may need another role's objectives to already exist to pick a valid target.
        var granted = new List<(EntityUid MindId, MindComponent Mind, CESecretRolePrototype Role)>();

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

                if (GrantSecretRole(rule, picked, role) is { } grantedMind)
                    granted.Add((grantedMind.MindId, grantedMind.Mind, role));
            }
        }

        foreach (var (mindId, mind, role) in granted)
            GrantSecretRoleObjectives(rule, mindId, mind, role);
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

            // Not batched like AssignSecretRoles - a single late joiner needs no deferral.
            if (GrantSecretRole(rule, session, role) is { } granted)
                GrantSecretRoleObjectives(rule, granted.MindId, granted.Mind, role);

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

    /// <summary>
    /// Grants the mind role and skills for a secret role - not objectives, callers create those separately.
    /// </summary>
    private (EntityUid MindId, MindComponent Mind)? GrantSecretRole(
        Entity<CESecretRoleSelectionComponent> rule,
        ICommonSession session,
        CESecretRolePrototype role)
    {
        if (!_mind.TryGetMind(session, out var mindId, out var mind))
            return null;

        _role.MindAddRole(mindId, MindRoleSecret, mind, silent: true);

        if (!_role.MindHasRole<CESecretRoleComponent>((mindId, mind), out var roleEnt))
            return null;

        roleEnt.Value.Comp2.Role = role.ID;
        rule.Comp.AssignedCounts[role.ID] = GetAssignedCount(rule, role.ID) + 1;

        if (role.Briefing is { } briefing)
            EnsureComp<RoleBriefingComponent>(roleEnt.Value.Owner).Briefing = briefing;

        if (session.AttachedEntity is { } target)
            GrantSecretRoleSkills(target, role);

        return (mindId, mind);
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
