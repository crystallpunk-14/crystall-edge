using Content.Server._CE.GameTicking;
using Content.Server.Discord.DiscordLink;
using Content.Server.GameTicking;
using Content.Shared._CE.Murk.Components;
using Content.Shared.CCVar;
using NetCord;
using NetCord.Gateway;
using NetCord.Rest;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Timing;

namespace Content.Server._CE.Discord;

/// <summary>
/// Shows the player count and the round progress in the Discord bot status. English only, not localized.
/// </summary>
public sealed partial class CEDiscordBot : IPostInjectInit
{
    [Dependency] private DiscordLink _discordLink = default!;
    [Dependency] private IPlayerManager _playerManager = default!;
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IEntityManager _entManager = default!;
    [Dependency] private IGameTiming _timing = default!;

    private const int BarSegments = 5;

    /// <summary>
    /// Keeps float error from flipping a bar segment on an exact boundary.
    /// </summary>
    private const float SegmentEpsilon = 0.001f;

    // Discord rate limits presence updates.
    private static readonly TimeSpan UpdateInterval = TimeSpan.FromSeconds(15);

    // Set from the gateway thread, consumed on the main thread.
    private volatile bool _resend;

    private bool _ready;
    private string? _lastStatus;
    private TimeSpan _nextUpdate;

    public void Initialize()
    {
        _discordLink.OnReady += OnReady;
    }

    public void Shutdown()
    {
        _discordLink.OnReady -= OnReady;
    }

    private void OnReady()
    {
        _resend = true;
    }

    public void Update()
    {
        if (_resend)
        {
            // A new gateway session starts with an empty presence.
            _resend = false;
            _ready = true;
            _lastStatus = null;
            _nextUpdate = TimeSpan.Zero;
        }

        if (!_ready || _timing.RealTime < _nextUpdate)
            return;

        _nextUpdate = _timing.RealTime + UpdateInterval;

        var status = GetStatus();
        if (status == _lastStatus)
            return;

        _lastStatus = status;

        var presence = new PresenceProperties(UserStatusType.Online)
        {
            Activities = new[]
            {
                new UserActivityProperties(status, UserActivityType.Custom)
                {
                    State = status,
                },
            },
        };

        _ = _discordLink.UpdatePresenceAsync(presence);
    }

    private string GetStatus()
    {
        var count = _playerManager.PlayerCount;
        var max = _cfg.GetCVar(CCVars.SoftMaxPlayers);
        var players = $"👥[{count}/{max}]";

        var round = GetRoundStatus();
        return round == null ? players : $"{players} | {round}";
    }

    /// <summary>
    /// The round part of the status, or null if there is nothing to tell about the round.
    /// </summary>
    private string? GetRoundStatus()
    {
        var runLevel = _entManager.System<GameTicker>().RunLevel;
        if (runLevel == GameRunLevel.PreRoundLobby)
            return "In lobby";

        var hasProgress = _entManager.System<CEMurkConsumingRuleSystem>()
            .TryGetRoundProgress(out var state, out var light, out var murk);

        if (hasProgress && state == CEMurkSphereState.Success)
            return "☀️ Light prevails";

        if (hasProgress && state == CEMurkSphereState.Failure)
            return "🌑 Darkness prevails";

        if (runLevel == GameRunLevel.PostRound)
            return "Round over";

        if (!hasProgress)
            return null;

        if (state == CEMurkSphereState.Ritual)
            return "🕯️ Ritual in progress";

        // Light only counts full segments; murk rounds up, so its bar is full before the round ends.
        var lightSegments = (int) MathF.Floor(light * BarSegments + SegmentEpsilon);
        var murkSegments = (int) MathF.Ceiling(murk * BarSegments - SegmentEpsilon);

        return $"☀️{GetBar(lightSegments)} 🌑{GetBar(murkSegments)}";
    }

    private static string GetBar(int filled)
    {
        filled = Math.Clamp(filled, 0, BarSegments);
        return new string('▰', filled) + new string('▱', BarSegments - filled);
    }

    void IPostInjectInit.PostInject()
    {
    }
}
