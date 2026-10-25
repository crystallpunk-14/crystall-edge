using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server._CE.GameTicking;
using Content.Server.Administration.Logs;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Server.Mind;
using Content.Server.Players.PlayTimeTracking;
using Content.Server.Roles;
using Content.Shared._CE.Objectives.Components;
using Content.Shared._CE.Roles;
using Content.Shared._CE.Roundflow;
using Content.Shared.Database;
using Content.Shared.GameTicking;
using Content.Shared.Ghost.Components;
using Content.Shared.Mind;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Content.Shared.Roles.Jobs;
using Content.Shared.Station;
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

    [Dependency] private IAdminLogManager _adminLog = default!;
    [Dependency] private IPlayerManager _playerManager = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private MindSystem _mind = default!;
    [Dependency] private RoleSystem _role = default!;
    [Dependency] private PlayTimeTrackingManager _playTimeTracking = default!;
    [Dependency] private SharedJobSystem _jobs = default!;
    [Dependency] private SharedStationSpawningSystem _stationSpawning = default!;

    [SubscribeLocalEvent]
    private void OnJobsAssigned(RulePlayerJobsAssignedEvent args)
    {
        var query = QueryActiveRules();
        while (query.MoveNext(out var uid, out _, out var comp, out _))
        {
            AssignSecretRoles((uid, comp), args.Players, args.Profiles);
        }

        // Every player learns their secret role right as they spawn.
        foreach (var session in args.Players)
        {
            SendRolePopup(session);
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
            if (!TryAssignGuaranteedSecretRole((uid, roleSelection), args.Player)
                && !TryAssignLateJoinSecretRole((uid, roleSelection), args.Player, args.Profile, playerCount))
                continue;

            SendRolePopup(args.Player);
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
    /// <param name="overwriteOriginal">Whether the new role also replaces
    /// <see cref="CESecretRoleComponent.OriginalRole"/> - false keeps the first role ever granted.</param>
    /// <param name="keepObjectives">Whether the objectives owned under the old role stay alongside
    /// the new role's ones instead of being removed.</param>
    /// <param name="giveGear">Whether the new role's starting gear is equipped.</param>
    public bool TrySetSecretRole(ICommonSession session,
        ProtoId<CESecretRolePrototype> roleId,
        [NotNullWhen(false)] out string? error,
        bool removeSkills = true,
        bool overwriteOriginal = false,
        bool keepObjectives = false,
        bool giveGear = true)
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

        ProtoId<CESecretRolePrototype>? originalRole = null;
        if (!overwriteOriginal && _role.MindHasRole<CESecretRoleComponent>((mindId, mind), out var oldRoleEnt))
            originalRole = oldRoleEnt.Value.Comp2.OriginalRole ?? oldRoleEnt.Value.Comp2.Role;

        ClearSecretRole(mindId, mind, removeSkills, keepObjectives);
        if (GrantSecretRole(targetRule.Value, session, role, originalRole, giveGear) is { } granted)
            GrantSecretRoleObjectives(targetRule.Value, granted.MindId, granted.Mind, role);

        // A mid-round role change is always announced, cracked sphere or not.
        SendRolePopup(session);

        return true;
    }

    private void ClearSecretRole(EntityUid mindId, MindComponent mind, bool removeSkills = true, bool keepObjectives = false)
    {
        if (!_role.MindHasRole<CESecretRoleComponent>((mindId, mind), out var roleEnt))
            return;

        if (roleEnt.Value.Comp2.Role is { } oldRoleId)
        {
            _adminLog.Add(LogType.AntagSelection,
                LogImpact.High,
                $"{ToPrettyString(mindId):player} lost secret role {oldRoleId}");

            var rules = QueryActiveRules();
            while (rules.MoveNext(out _, out _, out var comp, out _))
            {
                if (comp.AssignedCounts.TryGetValue(oldRoleId, out var count) && count > 0)
                    comp.AssignedCounts[oldRoleId] = count - 1;
            }

            if (mind.OwnedEntity is { } target)
            {
                if (removeSkills)
                    RemoveSecretRoleSkills(target, oldRoleId);

                // Always re-added fresh by GrantSecretRole, so its startup re-sends every other
                // role icon to viewers whose department just changed.
                RemComp<CESecretRoleIconComponent>(target);
                RemComp<CESecretRoleOriginalIconComponent>(target);
            }
        }

        // Only ever removes objectives this mind actually owns (its personal role-pool draw) -
        // the department's shared ones belong to the department's own holder entity and just drop
        // off this mind's list on the RegenerateObjectiveList call below.
        if (!keepObjectives && TryComp<CEObjectiveHolderComponent>(mindId, out var holderComp))
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

        RaiseNetworkEvent(new CEScreenPopupShowEvent(role.LocalizedName,
            audioPath: new SoundPathSpecifier("/Audio/_CE/Announce/darkness_boom.ogg")), session);
    }

    /// <summary>
    /// Grants every player their job-guaranteed role (<see cref="CESecretRolePrototype.GuaranteedJobs"/>)
    /// or, failing that, their decided role (<see cref="DecideSecretRoles"/>), then creates objectives
    /// for all of them - exposed (rather than private) so integration tests can drive the whole
    /// round-start flow without needing a running GameRule/GameTicker.
    /// </summary>
    public void AssignSecretRoles(
        Entity<CESecretRoleSelectionComponent> rule,
        ICommonSession[] players,
        IReadOnlyDictionary<NetUserId, HumanoidCharacterProfile> profiles)
    {
        var granted = new List<(EntityUid MindId, MindComponent Mind, CESecretRolePrototype Role)>();

        // Guaranteed roles go first: holding a role is what keeps these players out of the draw below.
        foreach (var session in players)
        {
            if (HasSecretRole(session) || !TryGetGuaranteedSecretRole(session, out var role))
                continue;

            if (GrantSecretRole(rule, session, role) is { } grantedMind)
                granted.Add((grantedMind.MindId, grantedMind.Mind, role));
        }

        var decisions = DecideSecretRoles(rule, players, profiles);

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

    /// <summary>
    /// Finds the role guaranteed to a job via <see cref="CESecretRolePrototype.GuaranteedJobs"/>, if any.
    /// </summary>
    public bool TryGetGuaranteedSecretRole(ProtoId<JobPrototype> job, [NotNullWhen(true)] out CESecretRolePrototype? role)
    {
        foreach (var candidate in _proto.EnumeratePrototypes<CESecretRolePrototype>())
        {
            if (!candidate.GuaranteedJobs.Contains(job))
                continue;

            role = candidate;
            return true;
        }

        role = null;
        return false;
    }

    private bool TryGetGuaranteedSecretRole(ICommonSession session, [NotNullWhen(true)] out CESecretRolePrototype? role)
    {
        role = null;
        return _mind.TryGetMind(session, out var mindId, out _)
               && _jobs.MindTryGetJobId(mindId, out var job)
               && job is { } jobId
               && TryGetGuaranteedSecretRole(jobId, out role);
    }

    private bool TryAssignGuaranteedSecretRole(Entity<CESecretRoleSelectionComponent> rule, ICommonSession session)
    {
        if (!TryGetGuaranteedSecretRole(session, out var role))
            return false;

        if (GrantSecretRole(rule, session, role) is { } granted)
            GrantSecretRoleObjectives(rule, granted.MindId, granted.Mind, role);

        return true;
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
    /// <param name="originalRole">The role to keep as <see cref="CESecretRoleComponent.OriginalRole"/> -
    /// null makes <paramref name="role"/> the original.</param>
    /// <param name="giveGear">Whether the role's starting gear is equipped.</param>
    private (EntityUid MindId, MindComponent Mind)? GrantSecretRole(
        Entity<CESecretRoleSelectionComponent> rule,
        ICommonSession session,
        CESecretRolePrototype role,
        ProtoId<CESecretRolePrototype>? originalRole = null,
        bool giveGear = true)
    {
        if (!_mind.TryGetMind(session, out var mindId, out var mind))
            return null;

        _role.MindAddRole(mindId, MindRoleSecret, mind, silent: true);

        if (!_role.MindHasRole<CESecretRoleComponent>((mindId, mind), out var roleEnt))
            return null;

        var original = originalRole ?? role.ID;
        roleEnt.Value.Comp2.Role = role.ID;
        roleEnt.Value.Comp2.OriginalRole = original;
        Dirty(roleEnt.Value.Owner, roleEnt.Value.Comp2);

        _adminLog.Add(LogType.AntagSelection,
            LogImpact.High,
            $"{ToPrettyString(mindId):player} was granted secret role {role.ID}");
        rule.Comp.AssignedCounts[role.ID] = GetAssignedCount(rule, role.ID) + 1;

        if (session.AttachedEntity is { } target)
        {
            GrantSecretRoleSkills(target, role);
            if (giveGear)
                _stationSpawning.EquipStartingGear(target, role.Gear);

            // Roles granted on the round end screen show up already revealed, like the rest.
            var revealed = GameTicker.RunLevel == GameRunLevel.PostRound;

            var icon = EnsureComp<CESecretRoleIconComponent>(target);
            icon.Role = role.ID;
            icon.Revealed = revealed;
            Dirty(target, icon);

            if (original != role.ID)
            {
                var originalIcon = EnsureComp<CESecretRoleOriginalIconComponent>(target);
                originalIcon.Role = original;
                originalIcon.Revealed = revealed;
                Dirty(target, originalIcon);
            }

            var grantedEv = new CESecretRoleGrantedEvent(target, role.ID, original);
            RaiseLocalEvent(ref grantedEv);
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

/// <summary>
/// Broadcast whenever a body is granted a secret role - at round start, on late join or on a role change.
/// </summary>
/// <param name="Original">The original role kept through the change - equal to <paramref name="Role"/> if it never changed.</param>
[ByRefEvent]
public readonly record struct CESecretRoleGrantedEvent(
    EntityUid Body,
    ProtoId<CESecretRolePrototype> Role,
    ProtoId<CESecretRolePrototype> Original);
