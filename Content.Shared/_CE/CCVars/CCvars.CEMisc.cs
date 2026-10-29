using Content.Shared.Administration;
using Content.Shared.CCVar.CVarAccess;
using Robust.Shared.Configuration;

namespace Content.Shared.CCVar;

public sealed partial class CCVars
{
    /// <summary>
    /// Automatically shuts down the server outside of the CBT plytime. Shitcoded enough, but it's temporary anyway
    /// </summary>
    [CVarControl(AdminFlags.Admin)]
    public static readonly CVarDef<bool> CEClosedBetaTest =
        CVarDef.Create("game.closed_beta_test", false, CVar.SERVERONLY);

    /// <summary>
    ///     URL of the Discord webhook which will relay round end summary messages.
    /// </summary>
    [CVarControl(AdminFlags.Admin)]
    public static readonly CVarDef<string> CEDiscordRoundEndSummaryWebhook =
        CVarDef.Create("discord.round_end_summary_webhook", string.Empty, CVar.SERVERONLY | CVar.CONFIDENTIAL);

    /// <summary>
    /// How many seconds the main character may stay alive and SSD before its player stops earning achievements and statistics.
    /// </summary>
    [CVarControl(AdminFlags.Admin)]
    public static readonly CVarDef<float> CEAchievementsSSDThreshold =
        CVarDef.Create("achievements.ssd_threshold", 600f, CVar.SERVERONLY);
}
