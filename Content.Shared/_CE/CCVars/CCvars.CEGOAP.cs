using Content.Shared.Administration;
using Content.Shared.CCVar.CVarAccess;
using Robust.Shared.Configuration;

namespace Content.Shared.CCVar;

public sealed partial class CCVars
{
    /// <summary>
    /// Whether the CE GOAP AI system is enabled.
    /// </summary>
    [CVarControl(AdminFlags.VarEdit)]
    public static readonly CVarDef<bool> CEGOAPEnabled =
        CVarDef.Create("ce.goap.enabled", true);

    /// <summary>
    /// Maximum number of GOAP agents updated per tick.
    /// </summary>
    [CVarControl(AdminFlags.VarEdit)]
    public static readonly CVarDef<int> CEGOAPMaxUpdates =
        CVarDef.Create("ce.goap.max_updates", 128);
}
