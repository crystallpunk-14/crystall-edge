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
}
