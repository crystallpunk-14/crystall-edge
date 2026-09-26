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
using Content.Shared.Roles.Jobs;
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
    [Dependency] private SharedJobSystem _jobs = default!;

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
            {
                RemoveSecretRoleSkills(target, oldRoleId);
                RemComp<CESecretRoleIconComponent>(target);
            }
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

    /// <summary>
    /// Grants every player their decided role (<see cref="DecideSecretRoles"/>), then creates
    /// objectives for all of them - exposed (rather than private) so integration tests can drive
    /// the whole round-start flow without needing a running GameRule/GameTicker.
    /// </summary>
    public void AssignSecretRoles(
        Entity<CESecretRoleSelectionComponent> rule,
        ICommonSession[] players,
        IReadOnlyDictionary<NetUserId, HumanoidCharacterProfile> profiles)
    {
        var decisions = DecideSecretRoles(rule, players, profiles);

        var granted = new List<(EntityUid MindId, MindComponent Mind, CESecretRolePrototype Role)>();

        foreach (var session in players)
        {
            if (!decisions.TryGetValue(session.UserId, out var roleId) || !_proto.TryIndex(roleId, out var role))
                continue;

            if (GrantSecretRole(rule, session, role) is { } grantedMind)
                granted.Add((grantedMind.MindId, grantedMind.Mind, role));
        }

        foreach (var (mindId, mind, role) in granted)
            GrantSecretRoleObjectives(rule, mindId, mind, role);
    }

    /// <summary>
    /// Decides which secret role, if any, each player should receive this round. Pure - touches no
    /// minds/components, so it's directly unit-testable without spinning up a round. Resolves
    /// strictly by priority tier, globally, before moving to the next tier: a player who set even
    /// one role to High must never lose it to someone else's default-Low "don't care" pick for that
    /// same role, regardless of shuffle order.
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

        var assignedCounts = new Dictionary<ProtoId<CESecretRolePrototype>, int>(rule.Comp.AssignedCounts);
        var result = new Dictionary<NetUserId, ProtoId<CESecretRolePrototype>>();

        for (var tier = JobPriority.High; tier >= JobPriority.Low; tier--)
        {
            foreach (var session in available)
            {
                if (result.ContainsKey(session.UserId))
                    continue;

                if (!TryPickRoleAtTier(rule, assignedCounts, session, profiles[session.UserId], players.Length, tier, out var role))
                    continue;

                assignedCounts[role.ID] = assignedCounts.GetValueOrDefault(role.ID) + 1;
                result[session.UserId] = role.ID;
            }
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
    /// Picks a single player's own highest-priority role, checking tiers one at a time via
    /// <see cref="TryPickRoleAtTier"/> against the rule's live assigned counts. Only used for a lone
    /// late joiner - <see cref="DecideSecretRoles"/> handles the round-start batch, where tiers must
    /// be resolved globally across every player before moving to the next.
    /// </summary>
    private bool TryPickRole(
        Entity<CESecretRoleSelectionComponent> rule,
        IReadOnlyDictionary<ProtoId<CESecretRolePrototype>, int> assignedCounts,
        ICommonSession session,
        HumanoidCharacterProfile profile,
        int playerCount,
        [NotNullWhen(true)] out CESecretRolePrototype? role)
    {
        for (var tier = JobPriority.High; tier >= JobPriority.Low; tier--)
        {
            if (TryPickRoleAtTier(rule, assignedCounts, session, profile, playerCount, tier, out role))
                return true;
        }

        role = null;
        return false;
    }

    /// <summary>
    /// Picks a player's highest-<see cref="CESecretRoleSelectorEntry.Weight"/> role (random tie-break)
    /// among the ones they set to exactly <paramref name="tier"/> priority, that they're eligible
    /// for, and that still have room per <paramref name="assignedCounts"/>.
    /// </summary>
    private bool TryPickRoleAtTier(
        Entity<CESecretRoleSelectionComponent> rule,
        IReadOnlyDictionary<ProtoId<CESecretRolePrototype>, int> assignedCounts,
        ICommonSession session,
        HumanoidCharacterProfile profile,
        int playerCount,
        JobPriority tier,
        [NotNullWhen(true)] out CESecretRolePrototype? role)
    {
        var candidates = new List<(CESecretRoleSelectorEntry Entry, CESecretRolePrototype Role)>();

        foreach (var entry in rule.Comp.Roles)
        {
            if (!_proto.TryIndex(entry.Role, out var candidateRole))
                continue;

            // "Never" no longer exists in the UI, but old data or an untouched role should
            // still be treated as the Low floor rather than excluded.
            var priority = profile.SecretRolePriorities.GetValueOrDefault(entry.Role, JobPriority.Low);
            if (priority < JobPriority.Low)
                priority = JobPriority.Low;

            if (priority != tier)
                continue;

            if (assignedCounts.GetValueOrDefault(entry.Role) >= entry.GetTargetCount(playerCount))
                continue;

            if (!IsEligible(session, profile, candidateRole))
                continue;

            candidates.Add((entry, candidateRole));
        }

        if (candidates.Count == 0)
        {
            role = null;
            return false;
        }

        var maxWeight = candidates.Max(c => c.Entry.Weight);
        var topCandidates = candidates.Where(c => c.Entry.Weight == maxWeight).ToList();

        role = _random.Pick(topCandidates).Role;
        return true;
    }

    private bool IsEligible(ICommonSession session, HumanoidCharacterProfile profile, CESecretRolePrototype role)
    {
        if (role.ProhibitedJobs.Count > 0
            && _mind.TryGetMind(session, out var mindId, out _)
            && _jobs.MindTryGetJobId(mindId, out var job)
            && job is { } jobId
            && role.ProhibitedJobs.Contains(jobId))
            return false;

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
        {
            GrantSecretRoleSkills(target, role);

            var icon = EnsureComp<CESecretRoleIconComponent>(target);
            icon.Role = role.ID;
            Dirty(target, icon);
        }

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
