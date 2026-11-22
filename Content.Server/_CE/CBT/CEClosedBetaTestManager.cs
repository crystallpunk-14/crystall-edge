using Content.Server.Chat.Managers;
using Content.Server.Chat.Systems;
using Content.Server.Discord.DiscordLink;
using Content.Server.GameTicking;
using Content.Shared.CCVar;
using NetCord.Rest;
using Robust.Shared.Audio;
using Robust.Shared.Configuration;
using Robust.Shared.Timing;

namespace Content.Server._CE.CBT;

public interface ICEClosedBetaTestManager
{
    void Initialize();

    /// <summary>
    /// Called every frame, including frames while the simulation is paused (an empty server auto-pauses).
    /// </summary>
    void Update();

    /// <summary>
    /// Whether a round is allowed to start right now. Announces the refusal when it is not.
    /// </summary>
    bool CanStartRound();
}

/// <summary>
/// Closed beta test launch schedule. Temporary manager, will be removed after the closed beta test.
/// While enabled: rounds can only be started inside of the scheduled windows (running rounds are never interrupted),
/// the lobby countdown always shows the real time until the next window, the server language is switched to the
/// language of the upcoming window and the Discord role of that language is pinged before the window opens,
/// and unanswered ahelps ping the admin role of the current language.
/// Manual (open) launches are done by disabling <see cref="CCVars.CEClosedBetaTest"/>.
/// The same schedule is duplicated by hand in the crystallpunk-docs schedule page.
/// </summary>
/// <remarks>
/// This is a manager and not an entity system on purpose: an empty server pauses the simulation and entity systems
/// stop updating, while the schedule must keep working with nobody online.
/// </remarks>
public sealed partial class CEClosedBetaTestManager : ICEClosedBetaTestManager
{
    [Dependency] private IEntitySystemManager _entSys = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IChatManager _chatManager = default!;
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private DiscordLink _discordLink = default!;
    [Dependency] private ILogManager _logManager = default!;

    private static readonly TimeSpan MskOffset = TimeSpan.FromHours(3);
    private static readonly TimeSpan LanguageSwitchLead = TimeSpan.FromHours(3);
    private static readonly TimeSpan NotifyLead = TimeSpan.FromMinutes(30);

    // Round start is allowed slightly before the window opens, so the lobby countdown that expires
    // exactly at the window start can never lose a race with the wall clock.
    private static readonly TimeSpan StartTolerance = TimeSpan.FromSeconds(5);

    private const ulong NotifyChannelId = 1289228845538345030;

    private static readonly CELanguage Russian = new("ru-RU", 1231659870810406922, 1334476256825311252);
    private static readonly CELanguage English = new("en-US", 1231659924841566319, 1356531986952622110);

    // Times are Moscow time (UTC+3, no DST).
    private static readonly CEWindow[] Windows =
    [
        new(DayOfWeek.Wednesday, 18, 4, Russian),
        new(DayOfWeek.Saturday, 16, 4, Russian),
        new(DayOfWeek.Thursday, 16, 4, English),
        new(DayOfWeek.Sunday, 22, 4, English),
    ];

    private static readonly TimeSpan UpdateFrequency = TimeSpan.FromSeconds(1);

    private ISawmill _sawmill = default!;

    // Wall clock on purpose: IGameTiming.CurTime does not advance while the simulation is paused.
    private DateTime _nextUpdateTime = DateTime.MinValue;

    private bool _enabled;
    private bool _timerSynced;
    private DateTime _lastNotifiedStart;
    private DateTime _pendingNotifyStart;

    public void Initialize()
    {
        _sawmill = _logManager.GetSawmill("cbt");

        _cfg.OnValueChanged(CCVars.CEClosedBetaTest, value => _enabled = value, true);
    }

    public void Update()
    {
        var now = DateTime.UtcNow;

        if (!_enabled)
        {
            // Schedule was switched off: give the lobby its regular countdown back.
            if (_timerSynced)
            {
                _timerSynced = false;

                if (_entSys.TryGetEntitySystem<GameTicker>(out var ticker))
                    ticker.CEResetRoundStartTime();
            }

            return;
        }

        if (now < _nextUpdateTime)
            return;

        _nextUpdateTime = now + UpdateFrequency;

        UpdateLanguage(now);
        SyncLobbyTimer(now);
        NotifyDiscord(now);
    }

    public bool CanStartRound()
    {
        if (!_enabled)
            return true;

        var now = DateTime.UtcNow;

        if (IsWindowOpen(now + StartTolerance))
            return true;

        _chatManager.DispatchServerAnnouncement(Loc.GetString("ce-cbt-start-blocked"));

        // Whoever tried to start the round, the countdown must not keep ticking on an expired value.
        SyncLobbyTimer(now);
        return false;
    }

    private void SyncLobbyTimer(DateTime now)
    {
        if (!_entSys.TryGetEntitySystem<GameTicker>(out var ticker))
            return;

        if (!IsWindowOpen(now + StartTolerance))
        {
            var nextStart = GetNextStart(now);
            ticker.CESetRoundStartTime(_timing.CurTime + (nextStart - now));
            _timerSynced = true;
        }
        else if (_timerSynced)
        {
            // The window just opened: the round starts right now.
            _timerSynced = false;
            ticker.CESetRoundStartTime(_timing.CurTime);
        }
    }

    private void UpdateLanguage(DateTime now)
    {
        // The language of the latest window whose switch time (start minus lead) has already passed.
        CEWindow? target = null;
        var targetStart = DateTime.MinValue;

        foreach (var window in Windows)
        {
            var start = GetLatestStart(window, now + LanguageSwitchLead);

            if (target != null && start <= targetStart)
                continue;

            target = window;
            targetStart = start;
        }

        if (target is not { } language)
            return;

        // Unanswered ahelps ping the admins of the current server language.
        var adminRole = language.Language.AdminRoleId.ToString();
        if (_cfg.GetCVar(CCVars.DiscordAhelpMention) != adminRole)
            _cfg.SetCVar(CCVars.DiscordAhelpMention, adminRole);

        var curLang = _cfg.GetCVar(CCVars.ServerLanguage);
        var newLang = language.Language.Code;

        if (curLang == newLang)
            return;

        _cfg.SetCVar(CCVars.ServerLanguage, newLang);

        if (!_entSys.TryGetEntitySystem<ChatSystem>(out var chatSystem))
            return;

        var message = language.Language == Russian
            ? "WARNING: The server changes its language to Russian. For the changes to apply to your device, reconnect to the server."
            : "WARNING: The server changes its language to English. For the changes to apply to your device, reconnect to the server.";

        chatSystem.DispatchGlobalAnnouncement(
            message,
            announcementSound: new SoundPathSpecifier("/Audio/Effects/beep1.ogg"),
            sender: "Server"
        );
    }

    private void NotifyDiscord(DateTime now)
    {
        foreach (var window in Windows)
        {
            var start = GetNextStart(window, now);
            var remaining = start - now;

            // Anywhere between the lead time and the start: a late or failed attempt is retried until the window opens.
            if (remaining > NotifyLead || remaining <= TimeSpan.Zero)
                continue;

            if (start == _lastNotifiedStart || start == _pendingNotifyStart)
                continue;

            _pendingNotifyStart = start;
            SendDiscordNotification(window, start);
            return;
        }
    }

    private async void SendDiscordNotification(CEWindow window, DateTime startUtc)
    {
        try
        {
            if (!_discordLink.IsConnected)
                return;

            var unix = new DateTimeOffset(startUtc).ToUnixTimeSeconds();
            var roleId = window.Language.RoleId;

            // Plain text on purpose: the message is in the language of the window, not of the server.
            var text = window.Language == Russian
                ? $"<@&{roleId}> ЗБТ начнётся через пол часа! Старт: <t:{unix}:F> (<t:{unix}:R>)"
                : $"<@&{roleId}> The closed beta test starts in half an hour! Start: <t:{unix}:F> (<t:{unix}:R>)";

            var mentions = new AllowedMentionsProperties
            {
                AllowedRoles = [roleId],
            };

            await _discordLink.SendMessageAsync(NotifyChannelId, text, mentions);
            _lastNotifiedStart = startUtc;
        }
        catch (Exception e)
        {
            _sawmill.Error($"Failed to send closed beta test discord notification:\n{e}");
        }
        finally
        {
            // Not delivered (no connection or an error): the next update retries until the window opens.
            _pendingNotifyStart = DateTime.MinValue;
        }
    }

    private static bool IsWindowOpen(DateTime utc)
    {
        foreach (var window in Windows)
        {
            var start = GetLatestStart(window, utc);

            if (utc < start + TimeSpan.FromHours(window.DurationHours))
                return true;
        }

        return false;
    }

    private static DateTime GetNextStart(DateTime utc)
    {
        var best = DateTime.MaxValue;

        foreach (var window in Windows)
        {
            var start = GetNextStart(window, utc);

            if (start < best)
                best = start;
        }

        return best;
    }

    private static DateTime GetNextStart(CEWindow window, DateTime utc)
    {
        return GetLatestStart(window, utc) + TimeSpan.FromDays(7);
    }

    /// <summary>
    /// Latest start of the window that is not later than the given UTC time.
    /// </summary>
    private static DateTime GetLatestStart(CEWindow window, DateTime utc)
    {
        var msk = utc + MskOffset;
        var daysBack = ((int) msk.DayOfWeek - (int) window.Day + 7) % 7;
        var start = msk.Date.AddDays(-daysBack).AddHours(window.StartHour);

        if (start > msk)
            start = start.AddDays(-7);

        return start - MskOffset;
    }

    /// <param name="Code">Culture name of the server language.</param>
    /// <param name="RoleId">Discord role of the testers waiting for this language.</param>
    /// <param name="AdminRoleId">Discord role of the admins of this language, pinged by unanswered ahelps.</param>
    private readonly record struct CELanguage(string Code, ulong RoleId, ulong AdminRoleId);

    private readonly record struct CEWindow(DayOfWeek Day, int StartHour, int DurationHours, CELanguage Language);
}
