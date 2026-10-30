using System.Diagnostics.CodeAnalysis;
using Content.Server.Chat.Managers;
using Content.Server.Players.PlayTimeTracking;
using Content.Shared.CCVar;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server._CE.Humanoid;

/// <summary>
/// Server side of <see cref="SpeciesPrototype.Requirements"/>: a character of a species the player hasn't
/// unlocked is kept in the lobby instead of joining the round.
/// </summary>
public sealed partial class CESpeciesRequirementsSystem : EntitySystem
{
    [Dependency] private IChatManager _chat = default!;
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private PlayTimeTrackingManager _playTime = default!;

    /// <summary>
    /// Whether the player meets the requirements of the profile's species.
    /// </summary>
    public bool IsAllowed(ICommonSession session,
        HumanoidCharacterProfile profile,
        [NotNullWhen(false)] out FormattedMessage? reason)
    {
        reason = null;

        if (!_cfg.GetCVar(CCVars.GameRoleTimers) || !_proto.Resolve(profile.Species, out var species))
            return true;

        _playTime.TryGetTrackerTimes(session, out var playTimes);
        return JobRequirements.TryRequirementsMet(species.Requirements,
            playTimes ?? new Dictionary<string, TimeSpan>(),
            out reason,
            EntityManager,
            _proto,
            profile,
            session.UserId);
    }

    /// <summary>
    /// Tells the player why they stay in the lobby if the profile's species is locked for them.
    /// </summary>
    /// <returns>True if the player must not join with this profile.</returns>
    public bool TryRefuseJoin(ICommonSession session, HumanoidCharacterProfile profile)
    {
        if (IsAllowed(session, profile, out var reason))
            return false;

        var name = _proto.Resolve(profile.Species, out var species) ? Loc.GetString(species.Name) : profile.Species.Id;
        _chat.DispatchServerMessage(session,
            Loc.GetString("ce-species-locked-wait-in-lobby", ("species", name), ("reason", reason.ToString())));
        return true;
    }

    /// <summary>
    /// Adds every species with requirements to <paramref name="species"/> - for random characters made
    /// before the player's unlocks are known (new accounts).
    /// </summary>
    public static void AddRestrictedSpecies(HashSet<string> species, IPrototypeManager protoManager)
    {
        foreach (var proto in protoManager.EnumeratePrototypes<SpeciesPrototype>())
        {
            if (proto.Requirements is { Count: > 0 })
                species.Add(proto.ID);
        }
    }
}
