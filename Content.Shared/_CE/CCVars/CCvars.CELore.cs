using Robust.Shared.Configuration;

namespace Content.Shared.CCVar;

public sealed partial class CCVars
{
    /// <summary>
    /// Current year of the world
    /// </summary>
    public static readonly CVarDef<int> CECurrentYear = CVarDef.Create("lore.current_year", 295, CVar.SERVER);

}
