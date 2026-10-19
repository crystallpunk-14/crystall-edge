using Robust.Shared.Configuration;

namespace Content.Shared.CCVar;

public sealed partial class CCVars
{
    /// <summary>
    /// Forces the seed of the next wildlands generation pass. 0 = random seed each round.
    /// The seed actually used is always written to the server log.
    /// </summary>
    public static readonly CVarDef<int> CEWildlandsSeed =
        CVarDef.Create("wildlands.seed", 0, CVar.SERVERONLY);
}
