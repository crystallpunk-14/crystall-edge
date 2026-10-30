using Content.Server.Players.PlayTimeTracking;
using Content.Shared._CE.NewPlayer;
using Content.Shared.CCVar;
using Content.Shared.Ghost.Components;
using Content.Shared.Mind;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.GameStates;
using Robust.Shared.Player;

namespace Content.Server._CE.NewPlayer;

public sealed partial class CENewPlayerSystem : CESharedNewPlayerSystem
{
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private PlayTimeTrackingManager _playTime = default!;
    [Dependency] private SharedMindSystem _mind = default!;

    [SubscribeLocalEvent]
    private void OnPlayerAttached(PlayerAttachedEvent args)
    {
        if (HasComp<GhostComponent>(args.Entity) || !MarkNewPlayer(args.Player))
            return;

        EnsureComp<CENewPlayerComponent>(args.Entity);
    }

    [SubscribeLocalEvent]
    private void OnPlayerDetached(PlayerDetachedEvent args)
    {
        RemComp<CENewPlayerComponent>(args.Entity);
    }

    [SubscribeLocalEvent]
    private void OnGetStateAttempt(Entity<CENewPlayerComponent> ent, ref ComponentGetStateAttemptEvent args)
    {
        if (args.Player is { } viewer)
            args.Cancelled = IsNewPlayer(viewer);
    }

    protected override bool CanSee(EntityUid viewer)
    {
        return !_player.TryGetSessionByEntity(viewer, out var session) || !IsNewPlayer(session);
    }

    /// <summary>
    /// Decides whether the player is a new player and remembers a positive answer on their mind,
    /// keeping them one until the round ends even if they reach the playtime threshold mid-round.
    /// </summary>
    private bool MarkNewPlayer(ICommonSession session)
    {
        if (IsNewPlayer(session))
        {
            if (_mind.TryGetMind(session.UserId, out var mind))
                EnsureComp<CENewPlayerMindComponent>(mind.Value);

            return true;
        }

        return false;
    }

    /// <summary>
    /// Read-only, as it also runs while sending game states.
    /// </summary>
    private bool IsNewPlayer(ICommonSession session)
    {
        if (_mind.TryGetMind(session.UserId, out var mind) && HasComp<CENewPlayerMindComponent>(mind.Value))
            return true;

        return IsBelowThreshold(session);
    }

    private bool IsBelowThreshold(ICommonSession session)
    {
        var hours = _cfg.GetCVar(CCVars.CENewPlayerPlaytimeHours);
        if (hours <= 0 || !_playTime.TryGetTrackerTimes(session, out _))
            return false;

        return _playTime.GetOverallPlaytime(session) < TimeSpan.FromHours(hours);
    }
}
