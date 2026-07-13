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
    [Dependency] private IConsoleHost _consoleHost = default!;
    [Dependency] private GameTicker _ticker = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private ChatSystem _chatSystem = default!;
    [Dependency] private RoundEndSystem _roundEnd = default!;
    [Dependency] private IConfigurationManager _cfg = default!;

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

    // Р’С‹ РјРѕР¶РµС‚Рµ СЃРєР°Р·Р°С‚СЊ: Р­Рґ, С‚С‹ РµР±Р°РЅСѓР»СЃСЏ? Р­С‚Рѕ Р¶Рµ Р»СЋС‚С‹Р№ С‰РёС‚РєРѕРґ!
    // Р СЏ РІР°Рј РѕС‚РІРµС‡Сѓ: Р”Р°. РќРѕ СЃР°РјР° СЃРёСЃС‚РµРјР° РѕРіСЂР°РЅРёС‡РµРЅРёСЏ РІСЂРµРјРµРЅРё СЂР°Р±РѕС‚С‹ СЃРµСЂРІРµСЂР° - РІСЂРµРјРµРЅРЅР°СЏ С€С‚СѓРєР° РЅР° СЌС‚Р°Рї СЂР°Р·СЂР°Р±РѕС‚РєРё, РєРѕС‚РѕСЂР°СЏ Р±СѓРґРµС‚ СѓРґР°Р»РµРЅР°.
    // РњРЅРµ РїСЂРѕСЃС‚Рѕ Р»РµРЅСЊ РєР°Р¶РґС‹Р№ СЂР°Р· Р·Р°РїСѓСЃРєР°С‚СЊ Рё РІС‹РєР»СЋС‡Р°С‚СЊ СЃРµСЂРІРµСЂ СЂСѓС‡РєР°РјРё.
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
