using Robust.Shared.Configuration;

namespace Content.Shared.CCVar;

public sealed partial class CCVars
{
    /// <summary>
    ///     Whether the arrivals shuttle is enabled.
    /// </summary>
    public static readonly CVarDef<bool> CEArrivalsShuttles =
        CVarDef.Create("shuttle.ce_arrivals", true, CVar.SERVERONLY);
}
