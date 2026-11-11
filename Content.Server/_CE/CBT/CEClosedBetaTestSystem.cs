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

/// <summary>
/// Closed beta test launch schedule. Temporary system, will be removed after the closed beta test.
/// While enabled: rounds can only be started inside of the scheduled windows (running rounds are never interrupted),
/// the lobby countdown always shows the real time until the next window, the server language is switched to the
/// language of the upcoming window and the Discord role of that language is pinged before the window opens,
/// and unanswered ahelps ping the admin role of the current language.
/// Manual (open) launches are done by disabling <see cref="CCVars.CEClosedBetaTest"/>.
/// The same schedule is duplicated by hand in the crystallpunk-docs schedule page.
/// </summary>
public sealed partial class CEClosedBetaTestSystem : EntitySystem
{
    [Dependency] private GameTicker _ticker = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private ChatSystem _chatSystem = default!;
    [Dependency] private IChatManager _chatManager = default!;
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private DiscordLink _discordLink = default!;

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
        new(DayOfWeek.Sunday, 18, 4, English),
    ];

    private readonly TimeSpan _updateFrequency = TimeSpan.FromSeconds(1f);
    private TimeSpan _nextUpdateTime = TimeSpan.Zero;

    private bool _enabled;
    private bool _timerSynced;
    private DateTime _lastNotifiedStart;

    public override void Initialize()
    {
        base.Initialize();

        _cfg.OnValueChanged(CCVars.CEClosedBetaTest, value => _enabled = value, true);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_enabled)
        {
            // Schedule was switched off: give the lobby its regular countdown back.
            if (_timerSynced)
            {
                _timerSynced = false;
                _ticker.CEResetRoundStartTime();
            }

            return;
        }

        if (_timing.CurTime < _nextUpdateTime)
            return;

        _nextUpdateTime = _timing.CurTime + _updateFrequency;
        var now = DateTime.UtcNow;

        UpdateLanguage(now);
        SyncLobbyTimer(now);
        NotifyDiscord(now);
    }

    [SubscribeLocalEvent]
    private void OnRoundStartAttempt(ref CERoundStartAttemptEvent args)
    {
        if (!_enabled)
            return;

        var now = DateTime.UtcNow;

        if (IsWindowOpen(now + StartTolerance))
            return;

        args.Cancelled = true;
        _chatManager.DispatchServerAnnouncement(Loc.GetString("ce-cbt-start-blocked"));

        // Whoever tried to start the round, the countdown must not keep ticking on an expired value.
        SyncLobbyTimer(now);
    }

    private void SyncLobbyTimer(DateTime now)
    {
        if (!IsWindowOpen(now + StartTolerance))
        {
            var nextStart = GetNextStart(now);
            _ticker.CESetRoundStartTime(_timing.CurTime + (nextStart - now));
            _timerSynced = true;
        }
        else if (_timerSynced)
        {
            // The window just opened: the round starts right now.
            _timerSynced = false;
            _ticker.CESetRoundStartTime(_timing.CurTime);
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

        var message = language.Language == Russian
            ? "WARNING: The server changes its language to Russian. For the changes to apply to your device, reconnect to the server."
            : "WARNING: The server changes its language to English. For the changes to apply to your device, reconnect to the server.";

        _chatSystem.DispatchGlobalAnnouncement(
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

            if (remaining > NotifyLead || remaining <= NotifyLead - TimeSpan.FromMinutes(1))
                continue;

            if (start == _lastNotifiedStart)
                continue;

            _lastNotifiedStart = start;
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
        }
        catch (Exception e)
        {
            Log.Error($"Failed to send closed beta test discord notification:\n{e}");
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
