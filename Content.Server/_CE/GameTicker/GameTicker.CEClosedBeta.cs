using Content.Shared.GameTicking;

namespace Content.Server.GameTicking;

public sealed partial class GameTicker
{
    /// <summary>
    /// Moves the lobby countdown to the specified time (in <see cref="IGameTiming.CurTime"/> terms) and syncs it to clients.
    /// Does nothing outside of the pre-round lobby or when the countdown is already that close to the target.
    /// </summary>
    public void CESetRoundStartTime(TimeSpan time)
    {
        if (RunLevel != GameRunLevel.PreRoundLobby)
            return;

        if (Math.Abs((_roundStartTime - time).TotalSeconds) < 0.5)
            return;

        _roundStartTime = time;
        RaiseNetworkEvent(new TickerLobbyCountdownEvent(_roundStartTime, Paused));
    }

    /// <summary>
    /// Resets the lobby countdown to the regular lobby duration.
    /// </summary>
    public void CEResetRoundStartTime()
    {
        CESetRoundStartTime(_gameTiming.CurTime + LobbyDuration);
    }
}

/// <summary>
/// Raised by <see cref="GameTicker.StartRound"/> before the round starts. Set <see cref="Cancelled"/> to forbid it.
/// </summary>
[ByRefEvent]
public record struct CERoundStartAttemptEvent(bool Cancelled = false);
