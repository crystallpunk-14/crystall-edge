using Robust.Shared.Configuration;

namespace Content.Shared.CCVar;

public sealed partial class CCVars
{
    /// <summary>
    ///     Whether the arrivals shuttle is enabled.
    /// </summary>
    public static readonly CVarDef<bool> CEArrivalsShuttles =
        CVarDef.Create("shuttle.ce_arrivals", true, CVar.SERVERONLY);

    /// <summary>
    ///     Should powerful spells be restricted from being learned until a certain time has elapsed?
    /// </summary>
    public static readonly CVarDef<bool>
        CESkillTimers = CVarDef.Create("game.skill_timers", true, CVar.SERVER | CVar.REPLICATED);

    /// <summary>
    /// Automatically shuts down the server outside of the CBT plytime. Shitcoded enough, but it's temporary anyway
    /// </summary>
    public static readonly CVarDef<bool> CEClosedBetaTest =
        CVarDef.Create("game.closed_beta_test", false, CVar.SERVERONLY);

    /// <summary>
    ///     Controls if round-end window shows custom objective summary for antags
    /// </summary>
    public static readonly CVarDef<bool>
        CEGameShowBlueText = CVarDef.Create("game.showbluetext", true, CVar.ARCHIVE | CVar.REPLICATED);

    /// <summary>
    ///     URL of the Discord webhook which will relay round end summary messages.
    /// </summary>
    public static readonly CVarDef<string> CEDiscordRoundEndSummaryWebhook =
        CVarDef.Create("discord.round_end_summary_webhook", string.Empty, CVar.SERVERONLY | CVar.CONFIDENTIAL);
}
