using Content.Server.Chat.Systems;
using Content.Server.GameTicking;
using Content.Server.RoundEnd;
using Content.Shared.CCVar;
using Robust.Shared.Audio;
using Robust.Shared.Configuration;
using Robust.Shared.Console;
using Robust.Shared.Timing;

namespace Content.Server._CE.CBT;

public sealed partial class CEClosedBetaTestSystem : EntitySystem
{
    [Dependency] private readonly IConsoleHost _consoleHost = default!;
    [Dependency] private readonly GameTicker _ticker = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly ChatSystem _chatSystem = default!;
    [Dependency] private readonly RoundEndSystem _roundEnd = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    private TimeSpan _nextUpdateTime = TimeSpan.Zero;
    private readonly TimeSpan _updateFrequency = TimeSpan.FromSeconds(60f);

    private bool _enabled;

    public override void Initialize()
    {
        base.Initialize();

        _enabled = _cfg.GetCVar(CCVars.CEClosedBetaTest);
        _cfg.OnValueChanged(CCVars.CEClosedBetaTest,
            _ => { _enabled = _cfg.GetCVar(CCVars.CEClosedBetaTest); },
            true);
    }

    // Вы можете сказать: Эд, ты ебанулся? Это же лютый щиткод!
    // И я вам отвечу: Да. Но сама система ограничения времени работы сервера - временная штука на этап разработки, которая будет удалена.
    // Мне просто лень каждый раз запускать и выключать сервер ручками.
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_enabled || _timing.CurTime < _nextUpdateTime)
            return;

        _nextUpdateTime = _timing.CurTime + _updateFrequency;
        var now = DateTime.UtcNow;

        LanguageRule(now);
        LimitPlaytimeRule(now);
        ApplyAnnouncements(now);
    }

    private void LanguageRule(DateTime now)
    {
        var curLang = _cfg.GetCVar(CCVars.ServerLanguage);

        var ruDays = now.DayOfWeek is DayOfWeek.Tuesday or DayOfWeek.Thursday or DayOfWeek.Saturday;

        if (ruDays && curLang != "ru-RU")
        {
            _cfg.SetCVar(CCVars.ServerLanguage, "ru-RU");

            _chatSystem.DispatchGlobalAnnouncement(
                "WARNING: The server changes its language to Russian. For the changes to apply to your device, reconnect to the server.",
                announcementSound: new SoundPathSpecifier("/Audio/Effects/beep1.ogg"),
                sender: "Server"
            );
        }
        else if (!ruDays && curLang != "en-US")
        {
            _cfg.SetCVar(CCVars.ServerLanguage, "en-US");

            _chatSystem.DispatchGlobalAnnouncement(
                "WARNING: The server changes its language to English. For the changes to apply to your device, reconnect to the server.",
                announcementSound: new SoundPathSpecifier("/Audio/Effects/beep1.ogg"),
                sender: "Server"
            );
        }
    }

    private void LimitPlaytimeRule(DateTime now)
    {
        var isWeekend = now.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;

        var allowedRuPlaytime = isWeekend && now.Hour is >= 13 and < 17;
        var allowedEngPlaytime = isWeekend && now.Hour is >= 17 and < 21;

        if (isWeekend && (allowedRuPlaytime || allowedEngPlaytime))
        {
            if (_ticker.Paused)
                _ticker.TogglePause();
        }
        else
        {
            if (_ticker.RunLevel == GameRunLevel.InRound)
                _roundEnd.EndRound();

            if (!_ticker.Paused)
                _ticker.TogglePause();
        }
    }

    private void ApplyAnnouncements(DateTime now)
    {
        var ruDays = now.DayOfWeek is DayOfWeek.Tuesday or DayOfWeek.Thursday or DayOfWeek.Saturday;

        var timeMap = new (int Hour, int Minute, Action Action)[]
        {
            (18, 45, () =>
            {
                if (!ruDays)
                    return;

                _chatSystem.DispatchGlobalAnnouncement(
                    Loc.GetString("ce-cbt-close-15m"),
                    announcementSound: new SoundPathSpecifier("/Audio/Effects/beep1.ogg"),
                    sender: "Server"
                );
            }),
            (19, 0, () =>
            {
                if (!ruDays)
                    return;

                _consoleHost.ExecuteCommand("endround");
            }),
            (19, 2, () =>
            {
                if (!ruDays)
                    return;

                _consoleHost.ExecuteCommand("golobby");
            }),
            (20, 45, () =>
            {
                if (ruDays)
                    return;

                _chatSystem.DispatchGlobalAnnouncement(
                    Loc.GetString("ce-cbt-close-15m"),
                    announcementSound: new SoundPathSpecifier("/Audio/Effects/beep1.ogg"),
                    sender: "Server"
                );
            }),
            (20, 58, () =>
            {
                if (ruDays)
                    return;

                _consoleHost.ExecuteCommand("endround");
            }),
            (21, 00, () =>
            {
                if (ruDays)
                    return;

                _consoleHost.ExecuteCommand("golobby");
            }),
        };

        foreach (var (hour, minute, action) in timeMap)
        {
            if (now.Hour == hour && now.Minute == minute)
                action.Invoke();
        }
    }
}
